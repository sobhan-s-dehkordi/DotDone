using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DotDone.Data.Context;

namespace DotDone.Tests;

public static class TestDbContextFactory
{
    public static (DotDoneDbContext db, SqliteConnection connection) CreateInMemoryDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DotDoneDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new DotDoneDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
