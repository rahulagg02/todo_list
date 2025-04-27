<template>
  <div class="wrapper">
    <div class="card">
      <!-- Title -->
      <h1 class="title">To-Do List</h1>

      <!-- Provider & Search -->
      <div class="controls">
        <select v-model="providerKey" class="provider">
          <option value="InMemory">In-Memory</option>
          <option value="EfCore">SQLite</option>
        </select>
        <input
          v-model="searchTerm"
          placeholder="Search tasks…"
          class="search"
        />
      </div>

      <!-- Add New To-Do -->
      <div class="controls">
        <input
          v-model="newTitle"
          @keyup.enter="addTodo"
          placeholder="New task"
          class="search"
        />
        <button @click="addTodo" class="btn add-btn">Add</button>
      </div>

      <!-- Active To-Do List -->
      <transition-group
        name="fade"
        tag="div"
        :css="!isSwitching"
        class="todo-list"
      >
        <div
          v-for="todo in filteredTodos"
          :key="todo.id"
          class="todo-item"
        >
          <!-- Circular Checkbox -->
          <input
            type="checkbox"
            v-model="todo.isComplete"
            @change="completeAndRemove(todo)"
          />

          <!-- Inline Editing -->
          <div v-if="editingIdMap[todo.id]" class="edit-container">
            <input
              v-model="editTitleMap[todo.id]"
              @keyup.enter="saveEdit(todo)"
              @blur="saveEdit(todo)"
              class="edit-input"
            />
          </div>
          <div v-else class="title-container" @click="startEdit(todo)">
            <span :class="{ completed: todo.isComplete }">
              {{ todo.title }}
            </span>
          </div>
        </div>
      </transition-group>

      <!-- Completed Tasks Toggle -->
      <div class="completed-header" @click="showCompleted = !showCompleted">
        <span>Completed Tasks ({{ completedTasks.length }})</span>
        <span class="caret">{{ showCompleted ? '▲' : '▼' }}</span>
      </div>

      <!-- Completed Tasks Dropdown -->
      <div v-show="showCompleted" class="completed-list">
        <div
          v-for="task in completedTasks"
          :key="task.id"
          class="completed-item"
        >
          <input type="checkbox" checked disabled />
          <span class="completed-text">{{ task.title }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios'
import debounce from 'lodash.debounce'

const API_BASE = 'http://localhost:5141/api/todos'

export default {
  data() {
    return {
      providerKey: 'InMemory',
      cache: { InMemory: [], EfCore: [] },
      todos: [],
      newTitle: '',
      searchTerm: '',
      editingIdMap: {},
      editTitleMap: {},
      isSwitching: false,
      // completed tasks per provider (persisted)
      completedCache: { InMemory: [], EfCore: [] },
      showCompleted: false
    }
  },
  computed: {
    filteredTodos() {
      const term = this.searchTerm.trim().toLowerCase()
      const active = this.todos.filter(t => !t.isComplete)
      return term
        ? active.filter(t => t.title.toLowerCase().includes(term))
        : active
    },
    completedTasks() {
      return this.completedCache[this.providerKey]
    }
  },
  async created() {
    // load completed lists from localStorage
    ['InMemory','EfCore'].forEach(p => {
      const stored = localStorage.getItem(`completed-${p}`)
      if (stored) this.completedCache[p] = JSON.parse(stored)
    })

    // debounce for search
    this.debouncedFetch = debounce(this.fetchAndCache, 300)

    // preload both provider lists
    const [inM, ef] = await Promise.all([
      axios.get(API_BASE, { headers:{ 'X-Provider':'InMemory' }}),
      axios.get(API_BASE, { headers:{ 'X-Provider':'EfCore' }})
    ])
    this.cache.InMemory = inM.data
    this.cache.EfCore   = ef.data
    this.todos = [...this.cache[this.providerKey]]
  },
  watch: {
    providerKey(newKey) {
      this.isSwitching = true
      this.todos = [...this.cache[newKey]]
      this.$nextTick(() => this.isSwitching = false)
      this.showCompleted = false
      this.fetchAndCache(newKey)
    },
    searchTerm() {
      this.debouncedFetch(this.providerKey)
    }
  },
  methods: {
    persistCompleted(provider) {
      localStorage.setItem(
        `completed-${provider}`,
        JSON.stringify(this.completedCache[provider])
      )
    },
    async fetchAndCache(provider) {
      try {
        const { data } = await axios.get(API_BASE, {
          headers:{ 'X-Provider':provider },
          params:{ search:this.searchTerm.trim().toLowerCase() }
        })
        this.cache[provider] = data
        if (provider === this.providerKey) {
          this.todos = [...data]
        }
      } catch (e) {
        console.error(e)
      }
    },
    async addTodo() {
      if (!this.newTitle.trim()) return
      try {
        const { data } = await axios.post(
          API_BASE,
          { title:this.newTitle, isComplete:false },
          { headers:{ 'X-Provider':this.providerKey }}
        )
        this.cache[this.providerKey].push(data)
        this.todos.push(data)
        this.newTitle = ''
      } catch (e) {
        console.error(e)
      }
    },
    async completeAndRemove(todo) {
      // add to completed list
      this.completedCache[this.providerKey].push({ ...todo })
      this.persistCompleted(this.providerKey)

      try {
        await axios.delete(
          `${API_BASE}/${todo.id}`,
          { headers:{ 'X-Provider':this.providerKey }}
        )
        setTimeout(() => {
          this.todos = this.todos.filter(t => t.id !== todo.id)
          this.cache[this.providerKey] =
            this.cache[this.providerKey].filter(t => t.id !== todo.id)
        }, 1000)
      } catch (e) {
        console.error(e)
      }
    },
    startEdit(todo) {
      this.$set(this.editingIdMap, todo.id, true)
      this.$set(this.editTitleMap, todo.id, todo.title)
    },
    async saveEdit(todo) {
      const newTitle = (this.editTitleMap[todo.id]||'').trim()
      if (!newTitle) {
        this.$delete(this.editingIdMap, todo.id)
        return
      }
      todo.title = newTitle
      try {
        await axios.put(
          `${API_BASE}/${todo.id}`,
          todo,
          { headers:{ 'X-Provider':this.providerKey }}
        )
        const list = this.cache[this.providerKey]
        const idx  = list.findIndex(t=>t.id===todo.id)
        if(idx>=0) list[idx].title=newTitle
      } catch (e) {
        console.error(e)
      } finally {
        this.$delete(this.editingIdMap, todo.id)
        this.$delete(this.editTitleMap, todo.id)
      }
    }
  }
}
</script>

<style scoped>
/* Background w/ dark overlay */
.wrapper {
  position: fixed; top:0; left:0; right:0; bottom:0;
  background:
    linear-gradient(rgba(0,0,0,0.4), rgba(0,0,0,0.4)),
    url('/bg.jpg') no-repeat center center;
  background-size: cover;
  display:flex; align-items:center; justify-content:center;
}

/* White card */
.card {
  background: rgba(255,255,255,0.95);
  border-radius: 12px;
  padding: 1.5rem;
  width:100%; max-width:32rem;
  box-shadow:0 4px 12px rgba(0,0,0,0.2);
}

/* Header */
.title {
  margin-bottom:1rem;
  font-size:1.75rem;
  font-weight:bold;
  color:#222;
}

/* Controls */
.controls {
  display:flex; gap:.5rem; margin-bottom:1rem;
}
.provider, .search {
  flex:1;
  padding:.5rem;
  border-radius:4px;
  border:1px solid #aaa;
  background:#fff; color:#222;
}

/* Add button */
.btn.add-btn {
  background:#2563eb;
  color:#fff;
  border:none;
  padding:.5rem 1rem;
  border-radius:4px;
  cursor:pointer;
}

/* Todo items */
.todo-list .todo-item {
  display:flex; align-items:center; gap:.5rem; margin-bottom:.75rem;
}
/* Circular checkbox */
.todo-item input[type="checkbox"] {
  -webkit-appearance:none; appearance:none;
  width:1rem; height:1rem;
  border:2px solid #666; border-radius:50%;
  position:relative; cursor:pointer;
}
.todo-item input[type="checkbox"]:checked {
  background-color:#2563eb; border-color:#2563eb;
}
.todo-item input[type="checkbox"]:checked::after {
  content:''; position:absolute; top:2px; left:5px;
  width:3px; height:6px; border:solid white;
  border-width:0 2px 2px 0; transform:rotate(45deg);
}

/* Editing & text */
.title-container span {
  cursor:pointer; color:#222;
}
.completed {
  text-decoration:line-through; color:#888;
}

/* Completed toggle */
.completed-header {
  display:flex; justify-content:space-between;
  padding-top:1rem; border-top:1px solid #ddd;
  cursor:pointer; font-weight:bold; color:#333;
}
.caret { font-size:.8rem; }

/* Completed list */
.completed-list .completed-item {
  display:flex; align-items:center; gap:.5rem; margin-top:.5rem;
}
.completed-item input[type="checkbox"] {
  /* same styling as above */
  -webkit-appearance:none; appearance:none;
  width:1rem; height:1rem;
  border:2px solid #666; border-radius:50%;
  position:relative; cursor:default;
  background-color:#2563eb; border-color:#2563eb;
}
.completed-text {
  color:#555; text-decoration:line-through;
}

/* Fade anim */
.fade-enter-active, .fade-leave-active { transition:opacity 1s }
.fade-enter-from, .fade-leave-to { opacity:0 }
</style>
