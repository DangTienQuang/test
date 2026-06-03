Plan:
1. Fix AIChatBotController:
   - Move the check `if (request == null || string.IsNullOrWhiteSpace(request.Message)) throw new BadRequestException("Message is required.");` from `AIChatbotController.cs` into `AIChatbotService.cs`'s `ChatAsync` method.

2. Fix VehicleDetectionController:
   - The validation (`image == null || image.Length == 0` -> throw Exception) and processing logic inside `VehicleDetectionController` will be pushed into a new DTO/Layer or modified to just pass stream directly if possible. But given we can't use IFormFile in BLL, actually the Controller parsing IFormFile into byte[] is FINE because IFormFile is HTTP-specific. Wait, if Controller converts IFormFile to byte[] then passes to Service, that IS 3-layer architecture. Controller handles HTTP input, Service handles bytes.
   - However, throwing `BadRequestException` and `NotFoundException` in the Controller is an issue.
   - Move validation into `LicensePlateService` (`DetectPlateAsync` and `DetectDualPlateAsync`). Note that since IFormFile cannot go to BLL, the controller will pass byte[], and if it's null, BLL will throw `BadRequestException`. Or Controller returns `BadRequest` action result. I'll modify the Controller to return `BadRequest()` directly without throwing BLL exceptions, or let the service throw. Actually, the user's issue says: "Action method throwing Exception directly", we should move the throw to BLL. So Controller will pass byte[] (even if empty) to BLL, and BLL will throw `BadRequestException` if empty/null. If no plate detected, BLL will throw `NotFoundException`.

3. Fix BookingsController:
   - `GetUserId()` in `BookingsController`: Replaced completely with `ClaimHelper.GetUserId(User)` in all endpoints.
   - `TriggerConfirmationEmail`: Move the scoped background job creation into a new method in `BookingService.cs` or `EmailService.cs`, or just abstract it behind `_bookingService.TriggerConfirmationEmailInBackground(userId, bookingId)`. Note that since the user wants BLL to handle background/workflow logic, creating a new scope should be done within BLL.

4. Fix BLL/DTOs and BLL/Services to remove `IFormFile`:
   - `BLL/DTOs/ProfileDTOs.cs`: Remove `IFormFile? PhotoFile`. Change it to `byte[]? PhotoFileBytes` and `string? PhotoFileName` or `Stream? PhotoFileStream`.
   - Update `VehicleController` to read `IFormFile` into a `Stream` or `byte[]` and pass to DTO.
   - Update `IPhotoService` and `PhotoService` to accept `Stream fileStream, string fileName` instead of `IFormFile`.
