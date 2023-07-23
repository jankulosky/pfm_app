﻿using API.Data.Interfaces;
using API.DTOs;
using API.Helpers;
using API.Mappings;
using API.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace API.Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public CategoryRepository(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedList<CategoryDto>> GetCategoryList(PaginationParams paginationParams)
        {
            var query = _context.Categories
                .ProjectTo<CategoryDto>(_mapper.ConfigurationProvider)
                .AsNoTracking();

            return await PagedList<CategoryDto>.CreateAsync(query, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<List<Category>> ImportCategoriesFromFile(IFormFile csv)
        {
            using var streamReader = new StreamReader(csv.OpenReadStream());
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);

            csvReader.Context.RegisterClassMap<CategoryMapper>();

            var categories = csvReader.GetRecords<Category>().ToList();

            foreach (var category in categories)
            {
                var dbCategory = await _context.Categories.FindAsync(category.Code);

                if (dbCategory == null)
                {
                    _context.Categories.Add(category);
                }
                else
                {
                    dbCategory.ParentCode = category.ParentCode;
                    dbCategory.Name = category.Name;
                }
            }

            await _context.SaveChangesAsync();

            return categories;
        }
    }
}
