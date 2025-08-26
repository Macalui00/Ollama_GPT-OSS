# 💬 Chat + RAG con Ollama y .NET

Este proyecto implementa una API en **ASP.NET Core (.NET 8)** que se conecta con **Ollama** (modelos locales como `gpt-oss:20b`) y permite:

- Streaming de respuestas (`/api/chat/sse`, `/api/chat/stream`)  
- Generación completa (`/api/chat/generate`)  
- Integración con **RAG (Retrieval-Augmented Generation)** usando SQLite y `sqlite-vec` para embeddings.  
- UI simple en `wwwroot/index.html` para chatear vía SSE.

---

## 📦 Requisitos

- **Windows 10/11**, Linux o macOS  
- **.NET 8 SDK**  
- **Ollama** instalado → [ollama.ai/download](https://ollama.ai/download)  
- **SQLite** + extensión [`sqlite-vec`](https://github.com/asg017/sqlite-vec)  
- Al menos **16 GB RAM** si usás `gpt-oss:20b`  

---

## ⚙️ Instalación

### 1. Restaurar dependencias .NET
```bash
dotnet restore
