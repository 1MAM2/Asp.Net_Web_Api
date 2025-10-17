using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using productApi.Context;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly productDb _context;

    public CategoryController(productDb context)
    {
        _context = context;
    }

    // GET: api/category
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _context.Categories
            .AsNoTracking() // read-only sorgular için performans artışı
            .Where(c => !c.IsDeleted)
            .Select(c => new CategoryReadDTO
            {
                Id = c.Id,
                CategoryName = c.CategoryName
            })
            .ToListAsync();

        if (categories == null || categories.Count == 0) return NotFound("Kategori bulunamadı");

        return Ok(categories);
    }

    // GET: api/category/{catid}/products
    [HttpGet("{catid}/products")]
    public async Task<IActionResult> GetProductsByCategory(int catid)
    {
        // Projection kullanarak sadece gerekli alanları alıyoruz
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == catid && !p.IsDeleted && p.Stock > 5)
            .Select(p => new ProductReadDTO
            {
                Id = p.Id,
                ProductName = p.ProductName,
                Price = p.Price,
                Stock = p.Stock,
                ImgUrl = p.ImgUrl
            })
            .ToListAsync();

        if (products == null || products.Count == 0)
            return NotFound("Bu kategoriye ait ürün bulunamadı");

        return Ok(products);
    }

    // GET: api/category/{catid}
    [HttpGet("{catid}")]
    public async Task<IActionResult> GetCategoryById(int catid)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == catid && !c.IsDeleted)
            .Select(c => new CategoryReadDTO
            {
                Id = c.Id,
                CategoryName = c.CategoryName
            })
            .FirstOrDefaultAsync();

        if (category == null) return NotFound("Kategori bulunamadı");

        return Ok(category);
    }

    // POST: api/category
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDTO category)
    {
        if (string.IsNullOrWhiteSpace(category.CategoryName))
            return BadRequest("Kategori adı boş olamaz");

        var newCategory = new Category
        {
            CategoryName = category.CategoryName
        };

        _context.Categories.Add(newCategory);
        await _context.SaveChangesAsync();

        var categoryDto = new CategoryReadDTO
        {
            Id = newCategory.Id,
            CategoryName = newCategory.CategoryName
        };

        return CreatedAtAction(nameof(GetCategoryById), new { catid = categoryDto.Id }, categoryDto);
    }

    // DELETE: api/category/{catid}
    [HttpDelete("{catid}")]
    public async Task<IActionResult> DeleteCategory(int catid)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == catid);

        if (category == null) return NotFound("Kategori bulunamadı");

        category.IsDeleted = true; // soft delete
        foreach (var product in category.Products)
        {
            product.IsDeleted = true;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT: api/category/{catid}
    [HttpPut("{catid}")]
    public async Task<IActionResult> UpdateCategory(int catid, [FromBody] CategoryUpdateDTO category)
    {
        var oldCategory = await _context.Categories.FindAsync(catid);
        if (oldCategory == null) return NotFound("Kategori bulunamadı");

        if (!string.IsNullOrWhiteSpace(category.CategoryName))
        {
            oldCategory.CategoryName = category.CategoryName;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
