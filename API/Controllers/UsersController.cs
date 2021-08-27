using System.Security.Claims;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using API.Helpers;
using API.Extensions;

namespace API.Controllers
{
	[Authorize]
	public class UsersController : BaseApiController
	{
		private readonly IMapper _mapper;
		private readonly IPhotoService _photoService;
		private readonly IUnitOfWork _unitOfWork;
		public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
		{
			_unitOfWork = unitOfWork;
			_photoService = photoService;
			_mapper = mapper;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
		{
			var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.FindFirst(ClaimTypes.Name)?.Value);
			userParams.CurrentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
			var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);
			Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
			return Ok(users);
		}

		[HttpGet("{username}", Name = "GetUser")]
		public async Task<ActionResult<MemberDto>> GetUser(string username)
		{
			var user = await _unitOfWork.UserRepository.GetMemberAsync(username);
			return user;
		}


		[HttpPost("add-photo")]
		public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
		{
			var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.FindFirst(ClaimTypes.Name)?.Value);
			var result = await _photoService.AddPhotoAsync(file);
			if (result.Error != null)
			{
				return BadRequest(result.Error.Message);
			}

			var photo = new Photo
			{
				Url = result.SecureUrl.AbsoluteUri,
				PublicId = result.PublicId
			};

			user.Photos.Add(photo);

			if (await _unitOfWork.Complete())
			{
				return CreatedAtRoute("GetUser", new { username = user.UserName }, _mapper.Map<PhotoDto>(photo));
			}
			return BadRequest("Problem adding photo");

		}

		[HttpPut("set-main-photo/{photoId}")]
		public async Task<ActionResult> SetMainPhoto(int photoId)
		{
			var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.FindFirst(ClaimTypes.Name)?.Value);

			var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
			if (photo.IsMain) return BadRequest("This is already your main photo");

			var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
			if (currentMain != null)
			{
				currentMain.IsMain = false;
			}
			photo.IsMain = true;

			if (await _unitOfWork.Complete())
			{
				return NoContent();
			}
			return BadRequest("Error updating profile picture");

		}

		[HttpPut("remove-main-photo/")]
		public async Task<ActionResult> RemoveMainPhoto(int photoId)
		{
			var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.FindFirst(ClaimTypes.Name)?.Value);

			var photo = user.Photos.FirstOrDefault(x => x.IsMain);
			photo.IsMain = false;
			if (await _unitOfWork.Complete())
			{
				return NoContent();
			}
			return BadRequest("Error deleting your profile picture.");
		}

		[HttpPut]
		public async Task<ActionResult> UpdateUser(ProfileCompleteDto profileCompleteDto)
		{
			var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.FindFirst(ClaimTypes.Name)?.Value);
			var interestList = (await _unitOfWork.InterestRepository.GetInterestsByUserIdAsync(user.Id));
			var interestListStrings = interestList.Select(x => x.Title);
			var newInterests = profileCompleteDto.Interests.Except(interestListStrings).ToList();
			var remainingInterests = interestList.Where(x => profileCompleteDto.Interests.Any(a => a != x.Title)).ToList();
			//Add interest if profileCompleteDto has only added interest
			foreach (var item in newInterests)
			{
				if (!(await InterestExists(item)))
				{
					var interest = new Interest
					{
						Title = item
					};
					var appUserInterest = new AppUserInterest
					{
						User = user,
						Interest = interest
					};
					_unitOfWork.InterestRepository.Add(interest);
					user.UserInterests.Add(appUserInterest);
					continue;
				}
				var existingInterest = await _unitOfWork.InterestRepository.GetInterestByTitleAsync(item);
				var newAppUserInterest = new AppUserInterest
				{
					User = user,
					Interest = existingInterest
				};
				user.UserInterests.Add(newAppUserInterest);
			}


			//Delete removed interests
			foreach (var item in remainingInterests)
			{
				_unitOfWork.UserRepository.RemoveInterest(item, user.Id);
			}
			_mapper.Map(profileCompleteDto, user);
			_unitOfWork.UserRepository.Update(user);
			if (await _unitOfWork.Complete()) return NoContent();

			return BadRequest("Failed to update user");
		}

		private async Task<bool> InterestExists(string title)
		{
			return await _unitOfWork.InterestRepository.GetInterestByTitleAsync(title) != null;
		}
	}
}