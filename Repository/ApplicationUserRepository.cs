using BulkyWeb.Models;
using BulkyWeb.Repository.IRepository;
using BulkyWeb.Data;

namespace BulkyWeb.Repository
{
    public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
    {
        private ApplicationDbContext _db;
        public ApplicationUserRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
    }
}