using Server.DTOs.Responses;
using System.Collections;
using System.Collections.Generic;

public class GlobalRatingLeaderboardResponse
{
    public List<GlobalRatingResponse> Players { get; set; } =
        new List<GlobalRatingResponse>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPlayers { get; set; }

    public int TotalPages { get; set; }
}
