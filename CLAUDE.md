# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ProductReviews is an AI-powered Blazor WebAssembly application demonstrating modern product catalog and chat functionality. The app showcases secure identity integration, AI product knowledge chat powered by GitHub Models, and comprehensive review system with star ratings.

## Architecture

**Multi-Project Solution Structure:**
- `AppProduct/` - Main ASP.NET Core host providing Identity, API controllers, AI services, and database
- `AppProduct.Shared/` - Domain models shared across projects (Product, ProductReview, Brand, etc.)
- `AppProduct.Shared.Blazor/` - Reusable Blazor components, pages, and services using MudBlazor
- `AppProduct.Blazor/` - Blazor WebAssembly client integration

**Key Technologies:**
- .NET 9, Entity Framework Core with SQLite
- ASP.NET Core Identity for authentication
- MudBlazor for UI components
- OData for API queries with filtering/pagination
- Semantic Kernel + GitHub Models for AI chat
- CSV import/export functionality across entities

## Common Commands

### Build and Run
```bash
# Build entire solution
dotnet build

# Run the application
dotnet run --project AppProduct

# Clean build artifacts
dotnet clean
```

### Database Operations
```bash
# Navigate to main project
cd AppProduct

# Update database with migrations
dotnet ef database update

# Create new migration (if needed)
dotnet ef migrations add MigrationName

# Drop and recreate database (development)
dotnet ef database drop -f && dotnet ef database update
```

### AI Configuration (Optional)
```bash
# Configure GitHub Models API key for AI features
dotnet user-secrets set "GitHubAI:ApiKey" "ghp_xxx" --project AppProduct

# Other AI settings
dotnet user-secrets set "GitHubAI:ChatModel" "gpt-4o-mini" --project AppProduct
dotnet user-secrets set "GitHubAI:EmbeddingModel" "text-embedding-3-small" --project AppProduct
```

### Testing & Development
The application runs without AI credentials - chat functionality gracefully degrades to deterministic responses.

Browse to http://localhost:5000, register a user, and explore:
- Products, Brands, Categories, Features, Tags management
- Star rating system with breakdown dialogs
- CSV import/export for bulk operations
- AI-powered product chat (when configured)

## Key Architectural Patterns

### Data Layer
- Entity Framework Core with domain models in `AppProduct.Shared/Models/`
- OData endpoints for each entity providing filtering, sorting, pagination
- Bulk upsert operations for CSV imports with smart defaults

### Review System
- `ProductReview` model with `decimal?` Rating field (supports precise ratings)
- Star rating components: `StarRating.razor`, `ProductRatingDisplay.razor`, `ReviewInput.razor`
- Rating breakdown dialogs showing distribution and recent reviews
- Verified purchase badges and helpful votes tracking

### AI Integration
- Semantic Kernel configuration in `Configuration/SemanticKernelConfig.cs`
- Custom `GitHubOpenAIEmbeddingService` for Azure-hosted endpoint
- Fallback services when API key unavailable
- Product chat in `Services/ProductChatService.cs` with retrieval-augmented generation

### UI Components
- MudBlazor-based responsive design with dark/light theme
- Reusable components in `AppProduct.Shared.Blazor/Components/`
- Data grids with server-side pagination, filtering, and search
- CSV import/export UI with progress indicators

### Security
- ASP.NET Core Identity with role-based authorization
- Rate limiting configured for API endpoints
- User secrets for sensitive configuration
- CORS and security headers configured

## Development Notes

### Adding New Entities
1. Create model in `AppProduct.Shared/Models/`
2. Add DbSet to `AppProduct/Data/ApplicationDbContext.cs`
3. Create controller in `AppProduct/Controllers/` following OData pattern
4. Add service methods to `AppProduct.Shared.Blazor/Services/AppService.cs`
5. Create Blazor pages (List, Add, Update) following existing patterns

### Star Rating System
- Use `ProductRatingDisplay` for read-only rating displays with breakdown
- Use `ReviewInput` for new review submission forms
- Use `StarRating` for basic interactive rating input
- Rating calculations handle decimal values for precise averaging

### CSV Import/Export
- All main entities support CSV operations via bulk upsert endpoints
- Smart defaults applied to missing required fields
- Progress indicators and error handling in UI

### MudBlazor Version Compatibility
- Some components may show analyzer warnings for deprecated parameters
- `@bind-IsOpen` for dialogs may need updates to newer MudBlazor patterns
- DialogService recommended for complex dialogs over embedded dialog components

### Navigation
- Responsive navigation with top bar toggle + drawer modes
- Mobile-optimized horizontal scrolling for navigation items
- User preference persistence for navigation layout

The application is designed to be fully functional without AI credentials, making it easy to develop and test core functionality while providing enhanced features when AI services are available.