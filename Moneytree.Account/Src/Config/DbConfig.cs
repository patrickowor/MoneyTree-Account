namespace Moneytree.Account.Src.Config;
using Microsoft.EntityFrameworkCore;
using Moneytree.Account.Src.Entities;

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
    public DbSet<UserModel> Users { get; set;  }
}