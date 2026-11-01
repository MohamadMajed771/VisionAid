
using Microsoft.EntityFrameworkCore;
using VisionAid.Api.Data;
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

    public async Task<SessionResponseDto?> CreateAsync(
        CreateSessionRequestDto  request)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.UserId == request.UserId);
            //one record at least

        if (!userExists)
            return null;

        var session = new StreamSession
        {
            UserId = request.UserId,
            StartedAt = DateTime.UtcNow,
            Status = "Active"
        };

        _context.StreamSessions.Add(session);
        await _context.SaveChangesAsync();

        return MapToResponse(session);
    }

    public async Task<SessionResponseDto?> GetByIdAsync(
        int sessionId)
    {
        var session = await _context.StreamSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        return session is null ? null : MapToResponse(session);
    }

    public async Task<bool> EndAsync(int sessionId)
    {
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session is null)
            return false;

        if (session.Status == "Ended")
            return true;

        session.Status = "Ended";
        session.EndedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static SessionResponseDto    MapToResponse(
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
}
