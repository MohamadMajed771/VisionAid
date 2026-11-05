using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Data;
using VisionAid.Api.DTOs.Analysis;
using VisionAid.Api.DTOs.Sessions;
using VisionAid.Api.Models;
using VisionAid.Api.Services.Interfaces;

namespace VisionAid.Api.Services;

public class SessionService : ISessionService
{
    private readonly AppDbContext _context;

    public SessionService(AppDbContext context)
    {
        _context = context;
    }

    // CREATE SESSION
    public async Task<SessionResponseDto?> CreateAsync(int userId)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.UserId == userId);

        if (!userExists)
            return null;

        // A user can have only one active session.
        var activeSessionExists = await _context.StreamSessions
            .AnyAsync(s =>
                s.UserId == userId &&
                s.Status == "Active");

        if (activeSessionExists)
            throw new InvalidOperationException(
                "The user already has an active session.");

        var session = new StreamSession
        {
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            Status = "Active"
        };

        _context.StreamSessions.Add(session);
        await _context.SaveChangesAsync();

        return MapToResponse(session);
    }

    // GET ACTIVE SESSION FOR USER
    public async Task<SessionResponseDto?> GetActiveAsync(int userId)
    {
        var session = await _context.StreamSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.UserId == userId &&
                s.Status == "Active");

        return session is null ? null : MapToResponse(session);
    }

    // GET SESSION BY ID, ONLY IF IT BELONGS TO USER
    public async Task<SessionResponseDto?> GetByIdAsync(
        int sessionId,
        int userId)
    {
        var session = await _context.StreamSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.SessionId == sessionId &&
                s.UserId == userId);

        return session is null ? null : MapToResponse(session);
    }

    // END SESSION, ONLY IF IT BELONGS TO USER
    public async Task<bool> EndAsync(int sessionId, int userId)
    {
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s =>
                s.SessionId == sessionId &&
                s.UserId == userId);

        if (session is null)
            return false;

        if (session.Status == "Ended")
            return true;

        session.Status = "Ended";
        session.EndedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    // MAP ENTITY TO RESPONSE DTO
    private static SessionResponseDto MapToResponse(
        StreamSession session)
    {
        return new SessionResponseDto
        {
            SessionId = session.SessionId,
            UserId = session.UserId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Status = session.Status
        };
    }

    
public async Task<List<SessionResponseDto>> GetEndedAsync(int userId)
{
    var sessions = await _context.StreamSessions
        .AsNoTracking()
        .Where(s =>
            s.UserId == userId &&
            s.Status == "Ended")
        .OrderByDescending(s => s.StartedAt)
        .ToListAsync();

    return sessions
        .Select(MapToResponse)
        .ToList();
}
}