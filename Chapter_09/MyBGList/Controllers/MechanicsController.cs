﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using MyBGList.Extensions;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.ComponentModel.DataAnnotations;
using MyBGList.Attributes;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MechanicsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly ILogger<MechanicsController> _logger;

        private readonly IDistributedCache _distributedCache;

        public MechanicsController(
            ApplicationDbContext context,
            ILogger<MechanicsController> logger,
            IDistributedCache distributedCache)
        {
            _context = context;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        [HttpGet(Name = "GetMechanics")]
        [ResponseCache(CacheProfileName = "Any-60")]
        public async Task<RestDTO<Mechanic[]>> Get(
            [FromQuery] RequestDTO<MechanicDTO> input)
        {
            Mechanic[]? result = null;
            var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";
            if (!_distributedCache.TryGetValue<Mechanic[]>(cacheKey, out result))
            {
                var query = _context.Mechanics.AsQueryable();
                if (!string.IsNullOrEmpty(input.FilterQuery))
                    query = query.Where(b => b.Name.Contains(input.FilterQuery));
                query = query
                        .OrderBy($"{input.SortColumn} {input.SortOrder}")
                        .Skip(input.PageIndex * input.PageSize)
                        .Take(input.PageSize);
                result = await query.ToArrayAsync();
                _distributedCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
            }

            return new RestDTO<Mechanic[]>()
            {
                Data = result!,
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordCount = await _context.Mechanics.CountAsync(),
                Links = new List<LinkDTO> {
                    new LinkDTO(
                        Url.Action(
                            null,
                            "Mechanics",
                            new { input.PageIndex, input.PageSize },
                            Request.Scheme)!,
                        "self",
                        "GET"),
                }
            };
        }

        [HttpPost(Name = "UpdateMechanic")]
        [ResponseCache(CacheProfileName = "NoCache")]
        public async Task<RestDTO<Mechanic?>> Post(MechanicDTO model)
        {
            var mechanic = await _context.Mechanics
                .Where(b => b.Id == model.Id)
                .FirstOrDefaultAsync();
            if (mechanic != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    mechanic.Name = model.Name;
                mechanic.LastModifiedDate = DateTime.Now;
                _context.Mechanics.Update(mechanic);
                await _context.SaveChangesAsync();
            };

            return new RestDTO<Mechanic?>()
            {
                Data = mechanic,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(
                            Url.Action(
                                null,
                                "Mechanics",
                                model,
                                Request.Scheme)!,
                            "self",
                            "POST"),
                }
            };
        }

        [HttpDelete(Name = "DeleteMechanic")]
        [ResponseCache(CacheProfileName = "NoCache")]
        public async Task<RestDTO<Mechanic?>> Delete(int id)
        {
            var mechanic = await _context.Mechanics
                .Where(b => b.Id == id)
                .FirstOrDefaultAsync();
            if (mechanic != null)
            {
                _context.Mechanics.Remove(mechanic);
                await _context.SaveChangesAsync();
            };

            return new RestDTO<Mechanic?>()
            {
                Data = mechanic,
                Links = new List<LinkDTO>
                {
                    new LinkDTO(
                            Url.Action(
                                null,
                                "Mechanics",
                                id,
                                Request.Scheme)!,
                            "self",
                            "DELETE"),
                }
            };
        }
    }
}
