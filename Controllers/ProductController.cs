

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using productApi.Context;

namespace productApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly productDb _context;
        public ProductController(productDb context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
            .Where(p => !p.IsDeleted)
            .Include(c => c.Category)
            .Include(p => p.GalleryImages) // bu farklı bir tablo o yüzden include etmen gerekiyor.
            .ToListAsync(); // soft delete
            var productDto = products.Select(p => new ProductReadDTO
            {
                Id = p.Id,
                ProductName = p.ProductName,
                ImgUrl = p.ImgUrl,
                CategoryName = p.Category?.CategoryName,
                Price = p.Price,
                Discount = p.Discount,
                FinalPrice = p.Price * (1 - p.Discount),
                Description = p.Description,
                CategoryId = p.CategoryId,
                GalleryImages = p.GalleryImages.Select(productImages => productImages.ImageUrl).ToList(),
                Stock = p.Stock,

            });
            return Ok(productDto);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.GalleryImages)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();
            var productReadDTO = new ProductReadDTO
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Price = product.Price,
                Discount = product.Discount,
                FinalPrice = product.Price * (1 - product.Discount),
                ImgUrl = product.ImgUrl,
                CategoryName = product.Category?.CategoryName,
                Description = product.Description,
                CategoryId = product.CategoryId,
                GalleryImages = product.GalleryImages.Select(productImages => productImages.ImageUrl).ToList(),
                Stock = product.Stock,

            };
            return Ok(productReadDTO);
        }
        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductCreateDTO dto)
        {
            var product = new Product
            {
                ProductName = dto.ProductName,
                Price = dto.Price,
                ImgUrl = dto.ImgUrl,
                CategoryId = dto.CategoryId,
                Discount = dto.Discount / 100,
                Description = dto.Description,
                GalleryImages = dto.GalleryImages.Select(url => new ProductImage
                {
                    ImageUrl = url,
                }).ToList(),
                Stock = dto.Stock,
            };



            _context.Products.Add(product);


            await _context.SaveChangesAsync();

            var category = await _context.Categories.FindAsync(dto.CategoryId);

            var productReadDto = new ProductReadDTO
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Price = product.Price,
                Discount = product.Discount,
                FinalPrice = product.Price * (1 - product.Discount),
                ImgUrl = product.ImgUrl,
                CategoryName = category?.CategoryName,
                GalleryImages = product.GalleryImages.Select(img => img.ImageUrl).ToList(),
            };
            return CreatedAtAction(nameof(GetProductById), new { id = productReadDto.Id }, productReadDto);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductUpdateDTO dto)
        {
            var product = await _context.Products
            .Include(p => p.GalleryImages)
            .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound("Ürün bulunamadı");

            if (!string.IsNullOrEmpty(dto.ProductName))
                product.ProductName = dto.ProductName;

            if (!string.IsNullOrEmpty(dto.ImgUrl))
                product.ImgUrl = dto.ImgUrl;

            product.Price = dto.Price;

            product.CategoryId = dto.CategoryId;

            product.Discount = dto.Discount;

            product.Description = dto.Description;

            product.Stock = dto.Stock;

            if (dto.GalleryImages != null && dto.GalleryImages.Any())
            {
                _context.ProductImages.RemoveRange(product.GalleryImages);

                product.GalleryImages = dto.GalleryImages.Select(imgUrls => new ProductImage
                {
                    ImageUrl = imgUrls,
                    ProductId = product.Id
                }).ToList();
            }



            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            product.IsDeleted = true; // soft delete
                                      // _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        public class UpdateStockDTO { public int NewStock { get; set; } }
        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDTO dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound("Product not found");

            product.Stock = dto.NewStock;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Stock updated successfully", product });
        }
    }
}