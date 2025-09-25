# Product Reviews – AI-Powered Product Catalog & Chat

An end‑to‑end sample showcasing a modern Blazor WASM application with:

- Product, Brands, Categories, Features, Tags, Reviews (EF Core + SQLite by default)
- Secure Identity (ASP.NET Core Identity)
- AI Product Knowledge Chat powered by GitHub Models (OpenAI‑compatible endpoint)
- Fallback deterministic embeddings + graceful degradation when AI credentials are missing/invalid
- Minimal AI health probe endpoint
- OData + OpenAPI/Swagger support
- Responsive navigation (top bar toggle + drawer) with mobile refinements
- Dark / Light theme switching via MudBlazor
- CSV import & export for Products and Product Reviews (bulk upsert) using CsvHelper
- Aggregate product rating display (average, count, per‑review breakdown)

> Purpose: Demonstrate how to layer AI retrieval+chat over an existing domain model with minimal friction while keeping the app fully usable without AI.

---
## Quick Start

### 1. Prerequisites
- .NET 9 SDK
- SQLite (bundled / no external install strictly required)
- A GitHub Personal Access Token (classic) with `models:read` scope for AI features

### 2. Clone
```
git clone https://github.com/donpotts/ProductReviews.git
cd ProductReviews
```

### 3. Configure Secrets (AI)
Use user-secrets (local dev) so tokens never enter source control:
```
dotnet user-secrets init --project AppProduct
# Set your GitHub Models PAT (models:read scope)
dotnet user-secrets set "GitHubAI:ApiKey" "ghp_xxx" --project AppProduct
```
Optional overrides:
```
dotnet user-secrets set "GitHubAI:Endpoint" "https://models.inference.ai.azure.com" --project AppProduct
# Chat model
dotnet user-secrets set "GitHubAI:ChatModel" "gpt-4o-mini" --project AppProduct
# Embedding model
dotnet user-secrets set "GitHubAI:EmbeddingModel" "text-embedding-3-small" --project AppProduct
```
If secrets are not set the app still runs; AI chat degrades with a deterministic fallback message.

### 4. Database Migrations
```
cd AppProduct
# (If migrations not yet created you can add them - sample likely already has them.)
dotnet ef database update
```
A local SQLite DB file will be created (e.g., in `bin/Debug/...`).

### 5. Run
```
dotnet run --project AppProduct
```
Browse to: http://localhost:5000 (or the Kestrel-assigned port). Register a user, sign in, explore products, open Product Chat.

---
## Data Import / Export
CSV toolbar actions are available on both the Products and Product Reviews pages:

| Page | Actions | Notes |
|------|---------|-------|
| Products | Export CSV / Import CSV | Bulk upsert: existing rows matched by (Name + optional Id) update; new rows insert. Related collections (Category/Brand/etc.) not created during import (simplify demo). |
| Product Reviews | Export CSV / Import CSV | Bulk upsert by (ProductId + CustomerEmail + Title if Id missing). Adds or updates review fields (Rating, Text, HelpfulVotes, Verified flag). |

Implementation details:
- Uses `MudFileUpload` for client selection and `CsvHelper` for parsing.
- Headers are case‑insensitive; missing columns are ignored.
- Each import shows a snackbar summary: processed / added / updated.
- Server bulk endpoint returns structured counts.

Minimal column examples:
```
Products: Name,Description,Price,InStock,ReleaseDate
ProductReviews: ProductId,CustomerName,CustomerEmail,Rating,Title,ReviewText,ReviewDate,IsVerifiedPurchase
```

---
## Ratings Display
Products grid shows:
- Average rating (computed from associated reviews with a value)
- Total review count
- Optional breakdown panel (star distribution) rendered via `ProductRatingDisplay` (small star size for compact layout)
If a product has no reviews a muted "No reviews" label is shown.

---
## AI Integration Overview

| Concern | Implementation |
|---------|----------------|
| Chat Model | Semantic Kernel `AddOpenAIChatCompletion` pointed at GitHub Models endpoint |
| Embeddings | Custom `GitHubOpenAIEmbeddingService` (because embedding extension lacked endpoint override) hitting `/embeddings` |
| Fallback | Deterministic 32-d hash embedding + NoOp chat when key absent |
| Retrieval | In-memory cosine similarity over precomputed (or fallback) product embeddings |
| Safety | Hard system prompt restricts answers to catalog context only |
| Health Check | (Optional) `/api/ai/chat-test` (commented or available depending on branch) |

### Flow
1. On first chat invocation product records are loaded, embeddings generated and cached in a concurrent dictionary.
2. User question -> embedding -> top N products via cosine similarity.
3. System + context + constrained instructions -> chat completion.
4. Response sanitized (guard rails: rejects politics/weather/news/sports / unknown questions).
5. Fallback note appended if embeddings unavailable.

### Why Custom Embedding Client?
GitHub Models requires specifying its Azure-hosted endpoint; SK’s embedding extension (version in use) did not expose an endpoint parameter. A lightweight HTTP client implementation (OpenAI-compatible schema) resolves that while keeping the Kernel pipeline simple.

---
## Configuration Keys
| Key | Description | Default (if not supplied) |
|-----|-------------|---------------------------|
| `GitHubAI:ApiKey` | PAT for GitHub Models | (empty) -> fallback services |
| `GitHubAI:Endpoint` | Base URL for models | `https://models.inference.ai.azure.com` |
| `GitHubAI:ChatModel` | Chat model id | `gpt-4o-mini` |
| `GitHubAI:EmbeddingModel` | Embedding model id | `text-embedding-3-small` |

---
## Project Structure (Simplified)
```
AppProduct/                 -> Razor Pages Host (Identity, API, AI registration)
  Configuration/SemanticKernelConfig.cs
  Controllers/ProductChatController.cs
  Services/ProductChatService.cs
AppProduct.Shared/          -> Domain models & shared logic
AppProduct.Shared.Blazor/   -> Reusable MudBlazor components & pages (ProductChat.razor, NavMenu, etc.)
AppProduct.Blazor/          -> Blazor (WASM host integration if used)
```

---
## Key Files to Explore
- `SemanticKernelConfig.cs` – AI service registration, custom embedding service.
- `ProductChatService.cs` – Retrieval + prompt assembly logic.
- `ProductChat.razor` – UI for interactive Q&A, auto-expands latest answer.
- `NavMenu.razor` – Responsive navigation + mobile scroll improvements.
- `ListProduct.razor` / `ListProductReview.razor` – CSV import/export + bulk upsert logic.

---
## Product Chat Prompt Strategy
System Prompt enforces: "Only answer using provided product context." The runtime prompt encloses:
```
<system>...rules...</system>
<context>...top N product blocks...</context>
<user_question>...question...</user_question>
<instructions>...factual constraints...</instructions>
```
This keeps the model anchored and reduces hallucinations. Forbidden topical drift triggers a stock refusal message.

---
## Running Without AI Credentials
What happens:
- `IChatCompletionService` replaced by `NoOpChatCompletionService` (returns deterministic message)
- `ITextEmbeddingGenerationService` replaced by a hash-based fallback
- UI still functional; chat answers show configuration guidance

---
## Testing AI Connectivity
(Optional) If the `AiTestController` is enabled:
```
GET /api/ai/chat-test
Response: { chat_ok: true|false, response, model, error, unauthorized }
```
Failures:
- 401 -> invalid/expired PAT or missing `models:read`
- 404 on embeddings -> endpoint path mismatch (toggle between `/embeddings` and `/v1/embeddings` if needed)

---
## Security Notes
- Store PAT only in user secrets / environment variables (never commit).
- System prompt intentionally narrow to mitigate data leakage / off-topic injections.
- Identity default password rules can be hardened further (see `Program.cs` / Identity options configuration).
- Always validate any future file ingestion (PDFs, etc.) if extended.

---
## UI / UX Highlights
- Top bar + optional drawer; user toggle preserved.
- Mobile horizontal scroll prevents overflow clipping & preserves login/dark mode buttons.
- Auto-expand newest chat answer; collapsible history with product sources.
- Dark / light theme switching persists across interactions.
- Compact loading indicators (MudBlazor circular progress) for chat & CSV import state.

---
## Extending the Sample
| Goal | Suggestion |
|------|------------|
| Persist embeddings | Replace in-memory dict with a lightweight SQLite vector table |
| Streaming answers | Use `GetStreamingChatMessageContentsAsync` and incremental UI append |
| Multi-turn memory | Maintain per-user `ChatHistory` scoped to session or persisted in DB |
| Observability | Add structured logging around retrieval scores & token counts |
| Role-based prompts | Inject role claims into system prompt to tailor responses |
| Advanced CSV mapping | Allow custom column ↔ property mapping UI |
| Review moderation | Add sentiment + toxicity check before saving reviews |

---
## Troubleshooting
| Symptom | Cause | Fix |
|---------|-------|-----|
| "AI not available (invalid key)." | Missing / wrong PAT | Re-add secret, restart app |
| Embedding 404 | Path mismatch | Switch `/embeddings` ↔ `/v1/embeddings` in custom service |
| All answers generic | Embeddings failed -> fallback | Verify PAT scope & model ids |
| Chat 401 | Incorrect token or scope | Regenerate PAT with `models:read` |
| CSV import skipped rows | Missing key fields | Ensure required columns present & clean data |

Logs from `Microsoft.SemanticKernel.Connectors.OpenAI` can show token usage; enable verbose logging in `appsettings.Development.json` if needed.

---
## Sample API Snippets
C# (ask chat directly via controller):
```csharp
var (answer, sources) = await _productChatService.AskAsync("What products are in stock under $50?");
```
HTTP:
```
POST /api/chat/products
{ "question": "Show the newest products" }
```
Response includes `answer` + lightweight `sources` array.

---
## Contact
Questions, ideas, improvements – reach out:

**Email:** Don.Potts@DonPotts.com

Enjoy exploring and adapting this AI-enabled Blazor WASM sample! 🚀
