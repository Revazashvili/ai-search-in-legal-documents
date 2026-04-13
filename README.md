# AI Search in Legal Documents

A web platform for semantic legal document search that demonstrates the difference between traditional keyword search and AI-powered semantic search, with an optional RAG chatbot layer on top.

## Core Features

### Three search modes

**1. Keyword search (baseline)**
- Uses PostgreSQL full-text search with `tsvector` / `ts_rank`
- BM25-style ranking
- Matches exact terms and morphological variants
- Returns ranked list of documents

**2. Semantic search (AI-powered)**
- User query is embedded via OpenAI `text-embedding-3-small`
- Cosine similarity search against Qdrant vector store
- Returns ranked list of documents by semantic meaning, not exact words
- Finds relevant articles even when exact query terms are absent

**3. RAG answer (AI chatbot layer)**
- Same vector retrieval as semantic search (top 5 chunks)
- Retrieved chunks are passed as context to GPT-4o
- GPT-4o generates a natural language answer citing specific article numbers
- Streamed back to the user
- UI disclaimer: "This is not legal advice"
