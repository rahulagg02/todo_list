<!--
  To-Do Application (Vue 3)
-->
<template>
  <div class="wrapper">
    <div class="card">
      <h1 class="title">To-Do List</h1>

      <!-- Provider selector and search -->
      <div class="controls">
        <select v-model="providerKey" class="provider">
          <option value="InMemory">In-Memory</option>
          <option value="EfCore">SQLite</option>
        </select>
        <input v-model="searchTerm" placeholder="Search tasks…" class="search" />
      </div>

      <!-- Add new task -->
      <div class="controls">
        <input v-model="newTitle" @keyup.enter="addTodo" placeholder="New task" class="search" />
        <button @click="addTodo" class="btn add-btn">Add</button>
      </div>

      <!-- Active tasks list -->
      <transition-group name="fade" tag="div" :css="!isSwitching" class="todo-list">
        <div v-for="todo in filteredTodos" :key="todo.id" class="todo-item">
          <!-- Completed checkbox -->
          <input type="checkbox" v-model="todo.isComplete" @change="completeAndRemove(todo)" />

          <!-- Inline editing-->
          <div v-if="editingId === todo.id" class="edit-container">
            <input
              ref="editInput"
              v-model="editTitle"
              @keyup.enter="saveEdit(todo)"
              @blur="saveEdit(todo)"
              class="edit-input"
            />
          </div>

          <!-- Display title, click to enter edit mode -->
          <div v-else class="title-container" @click="startEdit(todo)">
            <span :class="{ completed: todo.isComplete }">{{ todo.title }}</span>
          </div>

          <!-- delete permanently -->
          <span class="trash-icon" @click.stop="deleteActive(todo)" title="Delete">🗑️</span>
        </div>
      </transition-group>

      <!-- Completed tasks toggle -->
      <div class="completed-header" @click="showCompleted = !showCompleted">
        <span>Completed Tasks ({{ completedTasks.length }})</span>
        <span class="caret">{{ showCompleted ? '▲' : '▼' }}</span>
      </div>

      <!-- Completed tasks dropdown -->
      <div v-show="showCompleted" class="completed-list">
        <div v-for="task in completedTasks" :key="task.id" class="completed-item">
          <input type="checkbox" checked @change="restoreTask(task)" />
          <span class="completed-text">{{ task.title }}</span>
          <span class="trash-icon" @click.stop="deleteCompleted(task)" title="Permanently Delete">🗑️</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
// Axios for HTTP requests, debounce to throttle search calls
import axios from 'axios'
import debounce from 'lodash.debounce'
const API_BASE = 'http://localhost:5141/api/todos'       //.net Web API URL

export default {
  data() {
    return {
      providerKey: 'InMemory',
      cache: { InMemory: [], EfCore: [] },
      todos: [],
      newTitle: '',
      searchTerm: '',
      // Inline edit state
      editingId: null,
      editTitle: '',
      isSwitching: false,
      completedCache: { InMemory: [], EfCore: [] },
      showCompleted: false
    }
  },
  computed: {
    filteredTodos() {
      // Filter tasks in search box
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
    // Restore completed lists from localStorage
    ['InMemory','EfCore'].forEach(p => {
      const stored = localStorage.getItem(`completed-${p}`)
      if (stored) this.completedCache[p] = JSON.parse(stored)
    })

    // Debounce search fetch
    this.debouncedFetch = debounce(this.fetchAndCache, 300)

    // Preload both provider lists from API
    const [inM, ef] = await Promise.all([
      axios.get(API_BASE, { headers: { 'X-Provider': 'InMemory' } }),
      axios.get(API_BASE, { headers: { 'X-Provider': 'EfCore' } })
    ])
    this.cache.InMemory = inM.data
    this.cache.EfCore = ef.data
    this.todos = [...this.cache[this.providerKey]]
  },
  watch: {
    // Avoiding any delay when provider changes,  refetch, reset dropdown
    providerKey(newKey) {
      // Instant swap without animation
      this.isSwitching = true
      this.todos = [...this.cache[newKey]]
      this.$nextTick(() => (this.isSwitching = false))
      this.showCompleted = false
      this.fetchAndCache(newKey)
    },
    // On searchTerm change, call debounced fetch
    searchTerm() {
      this.debouncedFetch(this.providerKey)
    }
  },
  methods: {
    persistCompleted(provider) {
      localStorage.setItem(`completed-${provider}`, JSON.stringify(this.completedCache[provider]))
    },
    // Fetch tasks
    async fetchAndCache(provider) {
      try {
        const { data } = await axios.get(API_BASE, {
          headers: { 'X-Provider': provider },
          params: { search: this.searchTerm.trim().toLowerCase() }
        })
        this.cache[provider] = data
        if (provider === this.providerKey) this.todos = [...data]
      } catch (e) {
        console.error(e)
      }
    },
    // Create a new task with POST
    async addTodo() {
      if (!this.newTitle.trim()) return
      try {
        const { data } = await axios.post(
          API_BASE,
          { title: this.newTitle, isComplete: false },
          { headers: { 'X-Provider': this.providerKey } }
        )
        this.cache[this.providerKey].push(data)
        this.todos.push(data)
        this.newTitle = ''
      } catch (e) {
        console.error(e)
      }
    },
    // Marked complete, moves to completedCache and delete on API
    async completeAndRemove(todo) {
      // Move to completed
      this.completedCache[this.providerKey].push({ ...todo })
      this.persistCompleted(this.providerKey)
      try {
        await axios.delete(`${API_BASE}/${todo.id}`, { headers: { 'X-Provider': this.providerKey } })
        setTimeout(() => {
          this.todos = this.todos.filter(t => t.id !== todo.id)
          this.cache[this.providerKey] = this.cache[this.providerKey].filter(t => t.id !== todo.id)
        }, 1000)
      } catch (e) {
        console.error(e)
      }
    },
    // Permanently delete task
    async deleteActive(todo) {
      try {
        await axios.delete(`${API_BASE}/${todo.id}`, { headers: { 'X-Provider': this.providerKey } })
        this.todos = this.todos.filter(t => t.id !== todo.id)
        this.cache[this.providerKey] = this.cache[this.providerKey].filter(t => t.id !== todo.id)
      } catch (e) {
        console.error(e)
      }
    },
    async deleteCompleted(task) {
      this.completedCache[this.providerKey] = this.completedCache[this.providerKey].filter(t => t.id !== task.id)
      this.persistCompleted(this.providerKey)
    },
    // Restore a completed task with POST
    async restoreTask(task) {
      this.completedCache[this.providerKey] = this.completedCache[this.providerKey].filter(t => t.id !== task.id)
      this.persistCompleted(this.providerKey)
      try {
        const { data } = await axios.post(
          API_BASE,
          { title: task.title, isComplete: false },
          { headers: { 'X-Provider': this.providerKey } }
        )
        this.cache[this.providerKey].push(data)
        this.todos.push(data)
      } catch (e) {
        console.error(e)
      }
    },
    // inline editing for a task
    startEdit(todo) {
      // Enter edit mode
      this.editingId = todo.id
      this.editTitle = todo.title
      this.$nextTick(() => {
        // Focus input
        const el = this.$refs.editInput
        if (Array.isArray(el)) {
          // When multiple refs
          const match = el.find(i => i.id === `edit-${todo.id}`)
          match && match.focus()
        } else if (el) {
          el.focus()
        }
      })
    },
    async saveEdit(todo) {
      const newTitle = this.editTitle.trim()
      if (!newTitle) { this.editingId = null; return }
      todo.title = newTitle
      try {
        await axios.put(`${API_BASE}/${todo.id}`, todo, { headers: { 'X-Provider': this.providerKey } })
        const list = this.cache[this.providerKey]
        const idx = list.findIndex(t => t.id === todo.id)
        if (idx >= 0) list[idx].title = newTitle
      } catch (e) {
        console.error(e)
      } finally {
        this.editingId = null
      }
    }
  }
}
</script>

<style scoped>
.wrapper { 
  position: fixed; top:0; left:0; right:0; bottom:0;
  background:
   linear-gradient(rgba(0,0,0,0.4), rgba(0,0,0,0.4)), url('/bg.jpg') no-repeat center center;
   background-size:
    cover; display:flex; align-items:center; justify-content:center; 
}
.card {
  background: rgba(255,255,255,0.95);
  border-radius:12px; 
  padding:1.5rem; 
  width:100%; 
  max-width:32rem; 
  box-shadow:0 4px 12px rgba(0,0,0,0.2); 
}

.title { 
  margin-bottom:1rem; 
  font-size:1.75rem; 
  font-weight:bold; 
  color:#222; 
}
.controls {
  display:flex; 
  gap:.5rem;
  margin-bottom:1rem; 
}

.provider, .search { 
  flex:1; padding:.5rem; 
  border-radius:4px; border:1px solid #aaa; 
  background:#fff; color:#222; 
}

.btn.add-btn { 
  background:#2563eb; color:#fff; 
  border:none; padding:.5rem 1rem; 
  border-radius:4px; cursor:pointer; 
}

/* Task item rows */
.todo-list .todo-item { 
  display:flex; align-items:center; 
  gap:.5rem; margin-bottom:.75rem; 
}

/* circular checkboxes */
.todo-item input[type="checkbox"] { 
  -webkit-appearance:none; appearance:none; 
  width:1rem; height:1rem; border:2px solid #666; 
  border-radius:50%; position:relative; 
  cursor:pointer; 
}

.todo-item input[type="checkbox"]:checked { 
  background-color:#2563eb; 
  border-color:#2563eb; 
}

.todo-item input[type="checkbox"]:checked::after { 
  content:''; position:absolute; top:2px; left:5px; width:3px; height:6px; 
  border:solid white; border-width:0 2px 2px 0; 
  transform:rotate(45deg); 
}

/* Task text */
.title-container span { 
  cursor:pointer; color:#222; 
}

.completed { 
  text-decoration:line-through; color:#888; 
}

.trash-icon { 
  margin-left:auto; cursor:pointer; font-size:1rem; 
}

.completed-header { 
  display:flex; justify-content:space-between; 
  padding-top:1rem; border-top:1px solid #ddd; 
  cursor:pointer; font-weight:bold; color:#333; 
}

.caret { 
  font-size:.8rem; }

.completed-list .completed-item { 
  display:flex; align-items:center; 
  gap:.5rem; margin-top:.5rem; 
}

.completed-item input[type="checkbox"] { 
  -webkit-appearance:none; appearance:none; 
  width:1rem; height:1rem; border:2px solid #666; 
  border-radius:50%; position:relative; cursor:pointer; 
  background-color:#2563eb; 
  border-color:#2563eb; 
}

.completed-text { 
  color:#555; 
  text-decoration:line-through; 
}

.fade-enter-active, .fade-leave-active { 
  transition:opacity 1s; 
}

/* Fade transitions */
.fade-enter-from, .fade-leave-to { 
  opacity:0; 
}

</style>
