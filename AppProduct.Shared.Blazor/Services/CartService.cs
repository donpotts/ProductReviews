using AppProduct.Shared.Models;

namespace AppProduct.Shared.Blazor.Services;

public class CartService
{
    private readonly List<CartProduct> _cartItems = new();
    public event Action? CartChanged;
    private readonly IStorageService _storageService;
    private const string CartStorageKey = "cart_products";

    public CartService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public IReadOnlyList<CartProduct> CartItems => _cartItems.AsReadOnly();
    public int CartCount => _cartItems.Sum(x => x.Quantity);

    public async Task InitializeAsync()
    {
        await LoadCartFromStorageAsync();
    }

    public async Task AddToCartAsync(CartProduct product)
    {
        var existing = _cartItems.FirstOrDefault(x => x.Id == product.Id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            _cartItems.Add(new CartProduct
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "No description provided",
                Price = product.Price,
                Quantity = 1
            });
        }
        await SaveCartToStorageAsync();
        CartChanged?.Invoke();
    }

    public async Task AddToCartAsync(Product product)
    {
        var cartProduct = new CartProduct
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description ?? "No description provided",
            Price = product.Price,
            Quantity = 1
        };
        await AddToCartAsync(cartProduct);
    }

    public async Task RemoveFromCartAsync(long id)
    {
        var item = _cartItems.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            _cartItems.Remove(item);
            await SaveCartToStorageAsync();
            CartChanged?.Invoke();
        }
    }

    public async Task ClearCartAsync()
    {
        _cartItems.Clear();
        await SaveCartToStorageAsync();
        CartChanged?.Invoke();
    }

    public async Task UpdateQuantityAsync(long id, int delta)
    {
        var item = _cartItems.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            if (delta > 0)
                item.Quantity++;
            else if (item.Quantity > 1)
                item.Quantity--;
            await SaveCartToStorageAsync();
            CartChanged?.Invoke();
        }
    }

    public async Task ReloadCartAsync()
    {
        await LoadCartFromStorageAsync();
    }

    private async Task LoadCartFromStorageAsync()
    {
        var storedCartItems = await _storageService.GetAsync<List<CartProduct>>(CartStorageKey);
        _cartItems.Clear();
        if (storedCartItems != null)
        {
            _cartItems.AddRange(storedCartItems);
        }
        CartChanged?.Invoke();
    }

    private async Task SaveCartToStorageAsync()
    {
        await _storageService.SetAsync(CartStorageKey, _cartItems);
    }
}