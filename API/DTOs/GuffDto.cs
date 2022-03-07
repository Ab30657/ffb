using System;
using System.Collections.Generic;

namespace API.DTOs
{
	public class GuffDto
	{
		public ICollection<CommentDto> Comments { get; set; }
		public ICollection<LikeDto> LikedUsers { get; set; }
		public DateTime DatePosted { get; set; }
		public string GuffContent { get; set; }
	}
}