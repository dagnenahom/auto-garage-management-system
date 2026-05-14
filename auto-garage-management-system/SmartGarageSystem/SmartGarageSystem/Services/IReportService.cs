using SmartGarageSystem.Models;
using SmartGarageSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGarageSystem.Services
{
    public interface IReportService
    {
        Task<StockReport> GetStockReportAsync();
        Task<StockBalanceReport> GetStockBalanceReportAsync();
    }
}
