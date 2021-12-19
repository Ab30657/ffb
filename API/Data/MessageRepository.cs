using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
	public class MessageRepository : IMessageRepository
	{
		private readonly DataContext _context;
		private readonly IMapper _mapper;
		public MessageRepository(DataContext context, IMapper mapper)
		{
			_mapper = mapper;
			_context = context;
		}
		public void AddMessage(Message message)
		{
			_context.Messages.Add(message);
		}

		public void DeleteMessage(Message message)
		{
			_context.Messages.Remove(message);
		}

		public async Task<Message> GetMessage(int id)
		{
			return await _context.Messages.FindAsync(id);
		}

		public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
		{
			var query = _context.Messages.OrderByDescending(x => x.MessageSent).AsQueryable();

			query = messageParams.Container switch
			{
				"Inbox" => query.Where(x => x.Recipient.UserName == messageParams.Username),
				"Outbox" => query.Where(x => x.Sender.UserName == messageParams.Username),
				_ => query.Where(x => x.Recipient.UserName == messageParams.Username && x.DateRead == null)
			};

			var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);
			return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
		}

		public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
		{
			var messages = await _context.Messages.Include(x => x.Sender).ThenInclude(x => x.Photos).Include(x => x.Recipient).ThenInclude(x => x.Photos).Where(x => x.RecipientUsername == currentUsername && x.SenderUsername == recipientUsername || x.RecipientUsername == recipientUsername && x.SenderUsername == currentUsername).OrderBy(x => x.MessageSent).ToListAsync();

			var unread = messages.Where(x => x.DateRead == null && x.Recipient.UserName == currentUsername).ToList();

			if (unread.Any())
			{
				foreach (var item in unread)
				{
					item.DateRead = DateTime.Now;
				}
				await _context.SaveChangesAsync();
			}

			return _mapper.Map<IEnumerable<MessageDto>>(messages);

		}



		public async Task<bool> SaveAllAsync()
		{
			return await _context.SaveChangesAsync() > 0;
		}
	}
}