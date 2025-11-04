public record AskRequest(string Question, int? Limit);
public record AskResponse(object Data, object Explain);