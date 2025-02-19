﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Algoserver.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Algoserver.API.Data.Repositories
{
    public class StatisticsRepository
    {
        private readonly AppDbContextFactory _dbContextFactory;

        public StatisticsRepository()
        {
            _dbContextFactory = new AppDbContextFactory();
        }

        public async Task AddNAAccountBalances(IEnumerable<NAAccountBalances> balances)
        {
            if (balances.Any())
            {
                using (var context = _dbContextFactory.CreateDbContext())
                {
                    context.NAAccountBalances.AddRange(balances);
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task AddNALogs(IEnumerable<NALogs> logs)
        {
            if (logs.Any())
            {
                var dictionary = new Dictionary<string, List<NALogs>>();
                foreach(var l in logs)
                {
                    if (!dictionary.ContainsKey(l.Account))
                    {
                        dictionary.Add(l.Account, new List<NALogs>());
                    }

                    if (!dictionary[l.Account].Any((_) => string.Equals(_.Data, l.Data, System.StringComparison.InvariantCultureIgnoreCase)))
                    {
                        dictionary[l.Account].Add(l);
                    }
                }

                using (var context = _dbContextFactory.CreateDbContext())
                {
                    var accounts = logs.Where((_) => _.Type == 2).Select((_) => _.Account).Distinct().ToList();
                    context.NALogs.RemoveRange(context.NALogs.Where((_) => _.Type == 2 && accounts.Contains(_.Account)));

                    foreach (var i in dictionary)
                    {
                        context.NALogs.AddRange(i.Value);
                    }
                    
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task<List<NALogs>> GetNALogs(string account)
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var result = await context.NALogs.Where((_) => _.Account == account).OrderByDescending((_) => _.Date).Take(100).ToListAsync();
                return result;
            }
        }

        public async Task<NAAccountBalances> GetLastAccountSaved()
        {
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var result = await context.NAAccountBalances.OrderByDescending((_) => _.Date).FirstOrDefaultAsync();
                return result;
            }
        }

        // public async Task AddRangeAsync(IEnumerable<Statistic> statistics)
        // {
        //     if (statistics != null && statistics.Any())
        //     {
        //         using (var context = _dbContextFactory.CreateDbContext())
        //         {
        //             context.Statistics.AddRange(statistics);
        //             await context.SaveChangesAsync();
        //         }
        //     }
        // }
    }
}
