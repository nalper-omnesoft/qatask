using Microsoft.EntityFrameworkCore;
using ApiService.Data;
using ApiService.Models;

namespace ApiService.Endpoints;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/todos").RequireAuthorization();

        group.MapGet("/", async (QaTaskDbContext db) =>
            await db.TodoItems.OrderBy(t => t.Id).ToListAsync());

        group.MapGet("/{id:int}", async (int id, QaTaskDbContext db) =>
            await db.TodoItems.FindAsync(id) is TodoItem todo
                ? Results.Ok(todo)
                : Results.NotFound());

        group.MapPost("/", async (TodoItem todo, QaTaskDbContext db) =>
        {
            todo.CreatedAt = DateTime.UtcNow;
            todo.UpdatedAt = DateTime.UtcNow;
            db.TodoItems.Add(todo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/todos/{todo.Id}", todo);
        });

        group.MapPut("/{id:int}", async (int id, TodoItem input, QaTaskDbContext db) =>
        {
            var todo = await db.TodoItems.FindAsync(id);
            if (todo is null) return Results.NotFound();

            todo.Title = input.Title;
            todo.IsComplete = input.IsComplete;
            todo.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(todo);
        });

        group.MapDelete("/{id:int}", async (int id, QaTaskDbContext db) =>
        {
            var todo = await db.TodoItems.FindAsync(id);
            if (todo is null) return Results.NotFound();

            db.TodoItems.Remove(todo);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
