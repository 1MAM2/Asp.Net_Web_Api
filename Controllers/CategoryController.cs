


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
    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _context.Categories
        .Where(c => !c.IsDeleted) //soft delete den dolayı
        .ToListAsync();
        if (categories == null || categories.Count == 0) return NotFound();

        var categoriesDTO = categories.Select(c => new CategoryReadDTO
        {
            Id = c.Id,
            CategoryName = c.CategoryName
        });
        return Ok(categoriesDTO);
    }
    [HttpGet("{catid}/products")]
    public async Task<IActionResult> GetProductsByCategory(int catid)
    {
        var products = await _context.Products
        .Where(p => p.CategoryId == catid && p.IsDeleted == false && p.Stock > 5) 
        .ToListAsync();
        if (products == null || products.Count == 0) return NotFound("Bu kategoriye ait ürün bulunamadı");
        return Ok(products);

    }
    [HttpGet("{catid}")]
    public async Task<IActionResult> GetCategoryById(int catid)
    {
        var category = await _context.Categories.FindAsync(catid);

        if (category == null) return NotFound("Kategori bulunamadı");

        var categoryDTO = new CategoryReadDTO
        {
            Id = category.Id,
            CategoryName = category.CategoryName,
        };

        return Ok(categoryDTO);
    }
    [HttpPost]
    public async Task<IActionResult> CreateCategory(CategoryCreateDTO category)
    {
        var newCategory = new Category
        {
            CategoryName = category.CategoryName
        };

        _context.Add(newCategory);
        await _context.SaveChangesAsync();

        var categoryDto = new CategoryReadDTO
        {
            Id = newCategory.Id,
            CategoryName = newCategory.CategoryName
        };

        return CreatedAtAction(nameof(GetCategoryById), new { catid = categoryDto.Id }, categoryDto);
    }
    [HttpDelete("{catid}")]
    public async Task<IActionResult> DeleteCategory(int catid)
    {
        var category = await _context.Categories
        .Include(c => c.Products)
        .FirstOrDefaultAsync(c => c.Id == catid);
        if (category == null) return NotFound("Kategori Bulunamadı");
        category.IsDeleted = true; // soft delete
        // _context.Categories.Remove(category);
        foreach (var product in category.Products) // bu kat ait tüm ürünlere soft delete atıldı.
        {
            product.IsDeleted = true;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPut("{catId}")]
    public async Task<IActionResult> UpdateCategory(CategoryUpdateDTO category, int catId)
    {
        // if (category.Id != catId) return BadRequest("Gelen id ile category id uyuşmadı");

        var oldCategory = await _context.Categories.FindAsync(catId);
        if (oldCategory == null) return NotFound();
        if (!string.IsNullOrEmpty(category.CategoryName))
        {
            oldCategory.CategoryName = category.CategoryName;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

}