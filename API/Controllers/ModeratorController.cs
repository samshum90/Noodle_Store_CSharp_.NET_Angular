using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireModeratorRole")]
    public class ModeratorController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        public ModeratorController(IMapper mapper, IUnitOfWork unitOfWork, IPhotoService photoService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _photoService = photoService;
        }
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<AdminOrderDto>>> GetOrders()
        {
            var orders = await _unitOfWork.OrderRepository.GetOrdersAsync();
            return Ok(_mapper.Map<IEnumerable<Order>, IEnumerable<AdminOrderDto>>(orders));
            
        }

        [HttpGet("order/{id}")]
        public async Task<ActionResult<AdminOrderDto>> GetOrder(int id)
        {
            var order = await _unitOfWork.OrderRepository.GetOrderByIdAsync(id);

            if (order == null)
            {
                return NotFound(id);
            }

            return _mapper.Map<AdminOrderDto>(order);
        }

        [HttpGet("product/{id}", Name = "GetProduct")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetProductByIdAsync(id);

            if (product == null)
            {
                return NotFound(id);
            }

            return _mapper.Map<ProductDto>(product);

        }
        
        [HttpPost("product")]
        public async Task<ActionResult<Product>> CreateProduct([FromForm] Product product)
        {
            var productCheck = await _unitOfWork.ProductRepository.GetProductByNameAsync(product.Name);

            if (productCheck != null)return BadRequest("Product name is taken");
            _unitOfWork.ProductRepository.AddProduct(product);
            if (await _unitOfWork.Complete()) return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, _mapper.Map<ProductDto>(product));

            return BadRequest("Failed to add product");
        }

        [HttpPut("product/{id}")]
        public async Task<ActionResult> UpdateProduct([FromForm] Product product)
        {
            var dbProduct = await _unitOfWork.ProductRepository.GetProductByIdAsync(product.Id);
            if (dbProduct == null)
            {
                return  NotFound(dbProduct);
            }

            _mapper.Map(product, dbProduct);

            _unitOfWork.ProductRepository.Update(dbProduct);

            if (await _unitOfWork.Complete()) return NoContent();
            
            return BadRequest("Failed to update product");
            
        }

        [HttpDelete("product/{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _unitOfWork.ProductRepository.DeleteProduct(product);
            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Failed to Delete product");
        }

        [HttpPost("product/add-photo/{productId}")]
        public async Task<ActionResult<ProductPhotoDto>> AddPhoto(int productId, IFormFile file)
        {
            var product = await _unitOfWork.ProductRepository.GetProductByIdAsync(productId);
            if (product == null) return NotFound();
            
            var result = await _photoService.AddPhotoAsync(file);
            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new ProductPhoto
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (product.Photos.Count == 0)
            {
                photo.IsMain = true;
            }

            product.Photos.Add(photo);

            if (await _unitOfWork.Complete())
            {
                return CreatedAtRoute("GetProduct", new { id = productId }, _mapper.Map<ProductPhotoDto>(photo));
            }


            return BadRequest("Problem adding photo");
        }
        [HttpPut("product/set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var product = await _unitOfWork.ProductRepository.GetProductByPhotoIdAsync(photoId);
            if (product == null)
            {
                return NotFound();
            }
            var photo = product.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = product.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null) currentMain.IsMain = false;
            photo.IsMain = true;

            if (await _unitOfWork.Complete()) return NoContent();

            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("product/delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var product = await _unitOfWork.ProductRepository.GetProductByPhotoIdAsync(photoId);
            if (product == null)
            {
                return NotFound();
            }
            
            var photo = product.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo == null) return NotFound();

            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            product.Photos.Remove(photo);

            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}