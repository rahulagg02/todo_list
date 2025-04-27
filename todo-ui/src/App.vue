<template>
  <div style="padding:1.5rem; max-width:32rem; margin:0 auto; color:white;">
    <!-- Title -->
    <h1 style="text-align:left; margin-bottom:1rem; font-size:1.75rem; font-weight:bold;">
      To-Do List
    </h1>

    <!-- Provider & Search -->
    <div style="display:flex; gap:.5rem; margin-bottom:1rem;">
      <select
        v-model="providerKey"
        style="padding:.5rem; border:1px solid #555; background:#222; color:white;"
      >
        <option value="InMemory">In-Memory</option>
        <option value="EfCore">SQLite (EF Core)</option>
      </select>

      <input
        v-model="searchTerm"
        placeholder="Search tasks…"
        style="flex:1; padding:.5rem; border:1px solid #555; background:#222; color:white;"
      />
    </div>

    <!-- Add New To-Do -->
    <div style="display:flex; gap:.5rem; margin-bottom:1.5rem;">
      <input
        v-model="newTitle"
        @keyup.enter="addTodo"
        placeholder="New task"
        style="flex:1; padding:.5rem; border:1px solid #555; background:#222; color:white;"
      />
      <button
        @click="addTodo"
        style="padding:.5rem 1rem; background:#2563eb; color:white; border:none; border-radius:4px; cursor:pointer;"
      >
        Add
      </button>
    </div>

    <!-- To-Do List -->
    <transition-group
      name="fade"
      tag="div"
      :css="!isSwitching"
      style="margin:0; padding:0; width:100%;"
    >
      <div
        v-for="todo in filteredTodos"
        :key="todo.id"
        style="display:flex; align-items:center; justify-content:flex-start; text-align: left; gap:.5rem; margin-bottom:.75rem; width:100%;"
      >
        <!-- Complete Checkbox -->
        <input
          type="checkbox"
          v-model="todo.isComplete"
          @change="completeAndRemove(todo)"
        />

        <!-- Inline Editing -->
        <div v-if="editingIdMap[todo.id]" style="flex:1;">
          <input
            v-model="editTitleMap[todo.id]"
            @keyup.enter="saveEdit(todo)"
            @blur="saveEdit(todo)"
            style="width:100%; padding:.25rem; border:1px solid #555; background:#222; color:white;"
          />
        </div>
        <div v-else style="flex:1;">
          <span
            @click="startEdit(todo)"
            style="cursor:pointer; display:block; width:100%;"
            :style="{
              textDecoration: todo.isComplete ? 'line-through' : 'none',
              color: todo.isComplete ? '#888' : 'white'
            }"
          >
            {{ todo.title }}
          </span>
        </div>
      </div>
    </transition-group>
  </div>
</template>

<script>
import axios from 'axios'
import debounce from 'lodash.debounce'

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
      isSwitching: false,  // flag to disable transitions during provider switch
    }
  },
  computed: {
    filteredTodos() {
      const term = this.searchTerm.trim().toLowerCase()
      if (!term) return this.todos
      return this.todos.filter(t => t.title.toLowerCase().includes(term))
    }
  },
  async created() {
    // prepare debounced fetch for search
    this.debouncedFetch = debounce(this.fetchAndCache, 300)

    // preload both provider caches
    const [inM, ef] = await Promise.all([
      axios.get('http://localhost:5141/api/todos', { headers: { 'X-Provider': 'InMemory' }}),
      axios.get('http://localhost:5141/api/todos', { headers: { 'X-Provider': 'EfCore' }})
    ])
    this.cache.InMemory = inM.data
    this.cache.EfCore   = ef.data

    // initialize UI with the selected provider's cache
    this.todos = [...this.cache[this.providerKey]]
  },
  watch: {
    providerKey(newKey) {
      // disable animation, swap instantly
      this.isSwitching = true
      this.todos = [...this.cache[newKey]]
      // re-enable CSS next tick
      this.$nextTick(() => { this.isSwitching = false })
      // refresh in background
      this.fetchAndCache(newKey)
    },
    searchTerm() {
      // update filtering immediately, then fetch in background
      this.debouncedFetch(this.providerKey)
    }
  },
  methods: {
    async fetchAndCache(provider) {
      try {
        const { data } = await axios.get('http://localhost:5141/api/todos', {
          headers: { 'X-Provider': provider },
          params: { search: this.searchTerm.trim().toLowerCase() }
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
          'http://localhost:5141/api/todos',
          { title: this.newTitle, isComplete: false },
          { headers: { 'X-Provider': this.providerKey }}
        )
        this.cache[this.providerKey].push(data)
        this.todos.push(data)
        this.newTitle = ''
      } catch (e) {
        console.error(e)
      }
    },
    async completeAndRemove(todo) {
      try {
        await axios.delete(
          `http://localhost:5141/api/todos/${todo.id}`,
          { headers: { 'X-Provider': this.providerKey }}
        )
        setTimeout(() => {
          this.todos = this.todos.filter(t => t.id !== todo.id)
          this.cache[this.providerKey] = this.cache[this.providerKey].filter(t => t.id !== todo.id)
        }, 1000)
      } catch (e) {
        console.error(e)
      }
    },
    startEdit(todo) {
      this.editingIdMap[todo.id] = true
      this.editTitleMap[todo.id] = todo.title
    },
    async saveEdit(todo) {
      const newTitle = (this.editTitleMap[todo.id]||'').trim()
      if (!newTitle) {
        delete this.editingIdMap[todo.id]
        return
      }
      todo.title = newTitle
      try {
        await axios.put(
          `http://localhost:5141/api/todos/${todo.id}`,
          todo,
          { headers: { 'X-Provider': this.providerKey }}
        )
        const list = this.cache[this.providerKey]
        const idx  = list.findIndex(t => t.id === todo.id)
        if (idx >= 0) list[idx].title = newTitle
      } catch (e) {
        console.error(e)
      } finally {
        delete this.editingIdMap[todo.id]
        delete this.editTitleMap[todo.id]
      }
    }
  }
}
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 1s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
