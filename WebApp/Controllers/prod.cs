using Microsoft.AspNetCore.Mvc;
using Bulky;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace WebApp.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProductsController : ControllerBase
	{
		private readonly IProductRepository _productRepository;

		public ProductsController(IProductRepository productRepository)
		{
			_productRepository = productRepository;
		}

		[HttpGet]
		public IActionResult GetProducts()
		{
			var products = _productRepository.GetAll(includeProperties: "Category");
			return Ok(products);
		}

		[HttpGet("{id}")]
		public IActionResult GetProduct(int id)
		{
			var product = _productRepository.Get(p => p.ProductId == id, includeProperties: "Category");
			if (product == null)
			{
				return NotFound();
			}
			return Ok(product);
		}

		[HttpPost]
		public IActionResult CreateProduct([FromBody] Product product)
		{
			if (product == null)
			{
				return BadRequest();
			}

			_productRepository.Add(product);
			_productRepository.Save();

			return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
		}

		[HttpPut("{id}")]
		public IActionResult UpdateProduct(int id, [FromBody] Product product)
		{
			if (id != product.ProductId || product == null)
			{
				return BadRequest();
			}

			var existingProduct = _productRepository.Get(p => p.ProductId == id);
			if (existingProduct == null)
			{
				return NotFound();
			}

			existingProduct.Title = product.Title;
			existingProduct.Description = product.Description;
			existingProduct.Category_Id = product.Category_Id;
			// ... update other properties as needed

			_productRepository.Update(existingProduct);
			_productRepository.Save();

			return NoContent();
		}

		[HttpDelete("{id}")]
		public IActionResult DeleteProduct(int id)
		{
			var product = _productRepository.Get(p => p.ProductId == id);
			if (product == null)
			{
				return NotFound();
			}

			_productRepository.Remove(product);
			_productRepository.Save();

			return NoContent();
		}
	}
}
