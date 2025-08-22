namespace MockProvider.DTOs;

public record PayRequestDTO(decimal Total, string Curr, string Dest);

public record PayResponseDTO(bool Success, string TransactionId, string ProviderRef, DateTime ProcessedAt);


