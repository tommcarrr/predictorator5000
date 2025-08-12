using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Predictorator.Models;

namespace Predictorator.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
    public DbSet<SmsSubscriber> SmsSubscribers => Set<SmsSubscriber>();
    public DbSet<SentNotification> SentNotifications => Set<SentNotification>();
    public DbSet<GameWeek> GameWeeks => Set<GameWeek>();
}
