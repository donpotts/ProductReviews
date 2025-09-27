# 🚀 Product Reviews · AI-Powered Catalog & Chat Companion

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![Blazor WASM](https://img.shields.io/badge/Blazor-WASM-5C2D91?logo=blazor&logoColor=white)
![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-enabled-1C7ED6)

An opinionated sample that fuses AI-powered product knowledge chat with a modern Blazor application—fully functional even when AI credentials are missing.

> **Mission:** Showcase how to layer retrieval-augmented chat over an existing commerce domain with minimum friction and maximum developer clarity.

---

## 🧭 At a Glance

- 🧱 **Full catalog stack:** Products, Brands, Categories, Features, Tags, Reviews (EF Core + SQLite by default)
- 🛡️ **Secure identity:** ASP.NET Core Identity with ready-to-extend policies
- 🤖 **AI product concierge:** GitHub Models (OpenAI-compatible) with graceful fallbacks
- 🔄 **CSV import/export:** Bulk upsert via CsvHelper and MudBlazor tooling
- 🌓 **Responsive UX:** Dark/light theming, drawer navigation, mobile refinements
- 📊 **Ratings insights:** Aggregated stats + star breakdown per product

---

## 📚 Table of Contents

1. [Quickstart](#-quickstart)
2. [Tech Stack & Dependencies](#-tech-stack--dependencies)
3. [AI Configuration](#-ai-configuration)
4. [Data Import / Export](#-data-import--export)
5. [Ratings & UX Highlights](#-ratings--ux-highlights)
6. [AI Integration Deep Dive](#-ai-integration-deep-dive)
7. [Configuration Keys](#-configuration-keys)
8. [Folder Map](#-folder-map)
9. [Key Files](#-key-files)
10. [Extensibility Ideas](#-extensibility-ideas)
11. [Troubleshooting](#-troubleshooting)
12. [Sample API Calls](#-sample-api-calls)
13. [Contact](#-contact)

---

## ⚡ Quickstart

### 1. Prerequisites

- .NET 9 SDK
- SQLite (bundled; no external install required)
- GitHub Personal Access Token (classic) with `models:read` scope for AI features

### 2. Clone & Navigate

```
git clone https://github.com/donpotts/ProductReviews.git
cd ProductReviews
```

### 3. Configure Secrets (AI)

Use user-secrets so your PAT never lands in source control:

```
dotnet user-secrets init --project AppProduct
dotnet user-secrets set "GitHubAI:ApiKey" "ghp_xxx" --project AppProduct
```

Optional overrides:

```
dotnet user-secrets set "GitHubAI:Endpoint" "https://models.inference.ai.azure.com" --project AppProduct
dotnet user-secrets set "GitHubAI:ChatModel" "gpt-4o-mini" --project AppProduct
dotnet user-secrets set "GitHubAI:EmbeddingModel" "text-embedding-3-small" --project AppProduct
```

> **Heads-up:** No secrets? No problem. The UI stays fully functional while AI calls return a deterministic guidance message.

### 4. Apply Migrations

```
cd AppProduct
dotnet ef database update
```

SQLite spins up a local DB file under `bin/Debug/...`.

### 5. Run the App

```
dotnet run --project AppProduct
```

Visit http://localhost:5000 (or the assigned port), register, sign in, and open the Product Chat.

---

## � Tech Stack & Dependencies

### Core Framework
- **ASP.NET Core 9.0** — Web API and Razor Pages host
- **Blazor WebAssembly** — Interactive client-side UI
- **Entity Framework Core** — Data access layer with SQLite provider
- **ASP.NET Core Identity** — Authentication and authorization

### UI & Styling
- **MudBlazor** — Material Design component library
- **Bootstrap 5** — CSS framework for responsive design
- **Material Design Icons** — Icon set for UI elements

### AI & Machine Learning
- **Microsoft Semantic Kernel** — AI orchestration framework
- **GitHub Models** — OpenAI-compatible LLM hosting
- **Custom embedding service** — Vector similarity for product search

### Data Processing
- **CsvHelper** — CSV import/export operations
- **System.Text.Json** — JSON serialization
- **OData** — Query protocol for REST APIs

### Development & Tooling
- **Swashbuckle (Swagger)** — API documentation generation
- **Microsoft.Extensions.Hosting** — Application lifecycle management
- **Microsoft.Extensions.Configuration** — Configuration management
- **Microsoft.Extensions.Logging** — Structured logging

### Database
- **SQLite** — Lightweight file-based database
- **Entity Framework Migrations** — Database schema versioning

---

## �🤖 AI Configuration

| Concern | Implementation |
|---------|----------------|
| Chat completions | Semantic Kernel `AddOpenAIChatCompletion` targeting the GitHub Models endpoint |
| Embeddings | Custom `GitHubOpenAIEmbeddingService` to override the `/embeddings` path |
| Retrieval | In-memory cosine similarity over precomputed (or fallback) product embeddings |
| Safety | Guard-railed system prompt keeps answers within catalog context |
| Health check | Optional `/api/ai/chat-test` endpoint for diagnostics |

### Flow Overview

1. First chat request loads product data and caches embeddings.
2. User question → embedding → top-N product matches.
3. System prompt + curated context feed Semantic Kernel chat completion.
4. Responses sanitized; fallback note appended if embeddings are unavailable.

### Why a Custom Embedding Client?

GitHub Models mandate their Azure-hosted endpoint, but the SK embedding extension (at the time of writing) lacked an endpoint override. A lightweight OpenAI-compatible HTTP client bridges that gap without complicating the pipeline.

---

## 📥 Data Import / Export

CSV actions live on both the Products and Product Reviews pages:

| Page | Actions | Notes |
|------|---------|-------|
| Products | Export CSV / Import CSV | Bulk upsert by `(Name + optional Id)`; related collections aren’t created during import to keep the demo focused. |
| Product Reviews | Export CSV / Import CSV | Bulk upsert by `(ProductId + CustomerEmail + Title when Id missing)`; updates rating, text, votes, and verification status. |

Implementation details:

- Uses `MudFileUpload` for client-side file selection and `CsvHelper` for parsing.
- Header names are case-insensitive; missing columns are ignored gracefully.
- Import results surface via snackbar (processed / added / updated).
- Server endpoint returns structured counts for telemetry or logging.

Minimal column sets:

```
Products: Name,Description,Price,InStock,ReleaseDate
ProductReviews: ProductId,CustomerName,CustomerEmail,Rating,Title,ReviewText,ReviewDate,IsVerifiedPurchase
```

---

## ⭐ Ratings & UX Highlights

- Average rating + review count per product with “No reviews” fallback state
- Optional star-distribution panel via `ProductRatingDisplay` (compact star sizing)
- Responsive navigation bar + drawer combo with preserved user toggles
- Mobile-friendly horizontal scroll keeps login/theme toggles accessible
- Dark/light theme persistence and polished MudBlazor loading indicators

---

## 🧠 AI Integration Deep Dive

```
<system>Strictly answer using provided product context.</system>
<context>...top N product summaries...</context>
<user_question>...</user_question>
<instructions>...safety rails...</instructions>
```

Forbidden topics (politics, weather, news, sports, unknowns) trigger a stock refusal—keeping responses on-brand and on-scope.

---

## 🔑 Configuration Keys

| Key | Description | Default |
|-----|-------------|---------|
| `GitHubAI:ApiKey` | PAT for GitHub Models | (empty) → fallback services |
| `GitHubAI:Endpoint` | Base URL for models | `https://models.inference.ai.azure.com` |
| `GitHubAI:ChatModel` | Chat model Id | `gpt-4o-mini` |
| `GitHubAI:EmbeddingModel` | Embedding model Id | `text-embedding-3-small` |

---

## 🗂️ Folder Map (Simplified)

```
AppProduct/                 -> Razor Pages host (Identity, API, AI registration)
  Configuration/SemanticKernelConfig.cs
  Controllers/ProductChatController.cs
  Services/ProductChatService.cs
AppProduct.Shared/          -> Domain models & shared logic
AppProduct.Shared.Blazor/   -> Reusable MudBlazor components & pages
AppProduct.Blazor/          -> Optional Blazor WASM host integration
```

---

## 🔍 Key Files

- `Configuration/SemanticKernelConfig.cs` — AI service registration + custom embedding service
- `Services/ProductChatService.cs` — Retrieval and prompt assembly pipeline
- `ProductChat.razor` — Interactive Q&A UI with auto-expanding answers
- `NavMenu.razor` — Responsive navigation and mobile refinements
- `ListProduct.razor` / `ListProductReview.razor` — CSV import/export bulk logic

---

## 🌱 Extensibility Ideas

| Goal | Next Step |
|------|-----------|
| Persist embeddings | Introduce a lightweight SQLite vector table |
| Streaming answers | Use `GetStreamingChatMessageContentsAsync` for real-time UI updates |
| Multi-turn memory | Store per-user `ChatHistory` in session or DB |
| Observability | Log retrieval scores + token counts for diagnostics |
| Role-aware prompts | Blend identity claims into system prompt |
| Advanced CSV mapping | Allow admin-defined column ↔ property mapping |
| Review moderation | Add sentiment / toxicity guardrails before saving |

---

## 🛠️ Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| “AI not available (invalid key).” | Missing or incorrect PAT | Re-add secret via user-secrets and restart |
| Embedding 404 | Endpoint path mismatch | Toggle between `/embeddings` and `/v1/embeddings` |
| Generic answers | Fallback mode engaged | Verify PAT scope + model IDs |
| HTTP 401 for chat | Token scope missing `models:read` | Regenerate token with correct scope |
| CSV rows skipped | Missing key columns | Ensure required fields are present and clean |

Logs from `Microsoft.SemanticKernel.Connectors.OpenAI` reveal token usage—enable verbose logging in `appsettings.Development.json` when needed.

---

## 📮 Sample API Calls

**C#**

```csharp
var (answer, sources) = await _productChatService.AskAsync("What products are in stock under $50?");
```

**HTTP**

```
POST /api/chat/products
{ "question": "Show the newest products" }
```

The response includes the chatbot answer along with a lightweight `sources` array.

---

## 🤝 Contact

Questions, ideas, or looking to extend the sample? Drop a note:

**Email:** Don.Potts@DonPotts.com

Enjoy exploring and adapting this AI-enabled Blazor experience! �
