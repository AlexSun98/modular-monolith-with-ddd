# ADR-0022: RBAC with Permissions for Authorization

## Status

Accepted

## Context

Our application needs to control who can perform which actions. Different users have different levels of access:

- **Regular Members**: Can create meeting groups, attend meetings, comment
- **Meeting Group Organizers**: Can manage their meeting groups and meetings
- **Administrators**: Can approve meeting group proposals, manage users
- **System**: Background jobs and internal processes

### Requirements

1. **Fine-Grained Control**: Not just "admin vs user" but specific capabilities
2. **Flexible Roles**: Users can have multiple roles simultaneously
3. **Easy to Reason About**: Clear what each role can do
4. **Auditable**: Know who did what and with which permission
5. **Secure by Default**: Deny unless explicitly permitted
6. **Testable**: Easy to test authorization logic

### Considered Alternatives

1. **Simple Role-Based (Roles Only)**
   ```csharp
   [Authorize(Roles = "Admin")]
   ```
   - Pros: Simple, well-understood, built into ASP.NET Core
   - Cons: Inflexible, role explosion, hard to maintain

2. **Claims-Based**
   ```csharp
   [Authorize(Policy = "CanManageMeetings")]
   ```
   - Pros: Flexible, powerful
   - Cons: Policies scattered in code, harder to understand complete picture

3. **RBAC with Permissions**
   - User → Roles → Permissions
   - Check permissions, not roles
   - Pros: Flexible, maintainable, clear separation
   - Cons: More complexity than simple roles

## Decision

We will implement **Role-Based Access Control (RBAC) with Permissions**.

### Architecture

```
User
  ├─ Has Roles (1..*)
  │    ├─ Administrator
  │    ├─ MeetingOrganizer
  │    └─ Member
  │
  └─ Effective Permissions = Union of all role permissions
       ├─ GetMeetingDetails
       ├─ CreateMeeting
       ├─ ApproveMeetingGroupProposal
       └─ ...
```

### Model

**User → UserRole ← Role → RolePermission ← Permission**

```csharp
public class User
{
    public UserId Id { get; private set; }
    public string Login { get; private set; }
    private List<UserRole> _userRoles;
    
    public void AddRole(Role role)
    {
        _userRoles.Add(new UserRole(this.Id, role.Id));
    }
}

public class Role
{
    public string Name { get; private set; }
    private List<RolePermission> _permissions;
    
    public void AddPermission(Permission permission)
    {
        _permissions.Add(new RolePermission(this.Id, permission.Id));
    }
}

public class Permission
{
    public string Code { get; private set; }  // "GetMeetingDetails"
    public string Name { get; private set; }  // "Get Meeting Details"
}
```

### Permission Definitions

Each module defines its permissions:

```csharp
public static class MeetingsPermissions
{
    public const string GetAuthenticatedMemberMeetings = "GetAuthenticatedMemberMeetings";
    public const string GetMeetingDetails = "GetMeetingDetails";
    public const string CreateNewMeeting = "CreateNewMeeting";
    public const string EditMeeting = "EditMeeting";
    public const string AddMeetingAttendee = "AddMeetingAttendee";
    public const string RemoveMeetingAttendee = "RemoveMeetingAttendee";
    // ... more permissions
}

public static class AdministrationPermissions
{
    public const string GetMeetingGroupProposal = "GetMeetingGroupProposal";
    public const string AcceptMeetingGroupProposal = "AcceptMeetingGroupProposal";
    public const string GetAllMeetingGroupProposals = "GetAllMeetingGroupProposals";
    // ... more permissions
}

public static class PaymentsPermissions
{
    public const string GetPriceListItem = "GetPriceListItem";
    public const string CreatePriceListItem = "CreatePriceListItem";
    public const string BuySubscription = "BuySubscription";
    // ... more permissions
}
```

### Authorization at Controller Level

Always check **permissions**, never roles:

```csharp
[HttpGet("{meetingId}")]
[HasPermission(MeetingsPermissions.GetMeetingDetails)]  // ✅ Check permission
[ProducesResponseType(typeof(MeetingDetailsDto), StatusCodes.Status200OK)]
public async Task<IActionResult> GetMeetingDetails(Guid meetingId)
{
    var meetingDetails = await _meetingsModule.ExecuteQueryAsync(
        new GetMeetingDetailsQuery(meetingId));

    return Ok(meetingDetails);
}

[HttpPost]
[HasPermission(AdministrationPermissions.AcceptMeetingGroupProposal)]  // ✅ Check permission
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> AcceptMeetingGroupProposal(
    [FromBody] AcceptMeetingGroupProposalRequest request)
{
    await _administrationModule.ExecuteCommandAsync(
        new AcceptMeetingGroupProposalCommand(request.MeetingGroupProposalId));

    return Ok();
}
```

❌ **Never** check roles:
```csharp
[Authorize(Roles = "Admin")]  // ❌ Don't do this
```

### HasPermission Attribute

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public HasPermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userPermissions = context.HttpContext.User
            .Claims
            .Where(x => x.Type == "permission")
            .Select(x => x.Value)
            .ToList();

        if (!userPermissions.Contains(_permission))
        {
            context.Result = new ForbidResult();
        }
    }
}
```

### Authentication with IdentityServer

Using **OAuth2 Resource Owner Password Grant**:

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
&username=testMember@mail.com
&password=testMemberPass
&client_id=ro.client
&client_secret=secret
&scope=myMeetingsAPI openid profile
```

Response:
```json
{
    "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
    "token_type": "Bearer",
    "expires_in": 3600
}
```

### Token Claims

Token includes user permissions as claims:

```csharp
public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        var authResult = await _userAccessModule.ExecuteCommandAsync(
            new AuthenticateCommand(context.UserName, context.Password));

        if (!authResult.IsAuthenticated)
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                authResult.AuthenticationError);
            return;
        }

        // Include permissions as claims
        context.Result = new GrantValidationResult(
            authResult.User.Id.ToString(),
            "forms",
            authResult.User.Claims);  // Contains permission claims
    }
}
```

User claims include:

```csharp
public class UserContext
{
    public UserId Id { get; }
    public string Login { get; }
    
    public List<Claim> Claims
    {
        get
        {
            var claims = new List<Claim>
            {
                new Claim("sub", Id.Value.ToString()),
                new Claim("name", Login)
            };
            
            // Add permission claims
            foreach (var permission in _permissions)
            {
                claims.Add(new Claim("permission", permission.Code));
            }
            
            return claims;
        }
    }
}
```

### Default Roles and Permissions

**Member Role**: Basic functionality
- GetAuthenticatedMemberMeetings
- GetMeetingDetails
- GetMeetingAttendees
- AddMeetingComment
- ProposeMeetingGroup
- BuySubscription

**Organizer Role**: Meeting management (in addition to Member)
- CreateNewMeeting
- EditMeeting
- CancelMeeting
- AddMeetingAttendee
- RemoveMeetingAttendee

**Administrator Role**: Administrative tasks
- GetAllMeetingGroupProposals
- GetMeetingGroupProposal
- AcceptMeetingGroupProposal
- GetAllMembers
- CreatePriceListItem
- ActivatePriceListItem

## Consequences

### Positive

- **Fine-Grained Control**: Permissions provide precise access control
- **Flexibility**: Easy to add new permissions without changing roles
- **Maintainability**: Clear separation between roles and capabilities
- **Auditable**: Can track exactly which permission was used
- **Testable**: Easy to test by mocking user with specific permissions
- **Self-Documenting**: Permission constants document what actions exist
- **Secure by Default**: Must explicitly grant permission
- **Scalable**: New roles can be composed from existing permissions

### Negative

- **More Complex**: More entities (User, Role, Permission) than simple roles
- **Permission Explosion**: Many permissions to manage
- **Token Size**: Permissions in claims increase token size
- **Performance**: Need to query permissions on authentication
- **Administration UI**: Need UI to manage role-permission mappings

### Best Practices

1. **Always check permissions, never roles** at authorization points
2. **Define permissions as constants** for compile-time safety
3. **One permission per action** (don't reuse permissions)
4. **Descriptive names** that clearly state the capability
5. **Module prefix** to avoid conflicts (Meetings.*, Administration.*)
6. **Document permissions** alongside API endpoints

### Permission Naming Convention

```
{Module}.{Action}{Resource}

Examples:
- Meetings.GetMeetingDetails
- Meetings.CreateNewMeeting  
- Administration.AcceptMeetingGroupProposal
- Payments.BuySubscription
```

### Security Considerations

1. **Token in HTTP Header**: 
   ```
   Authorization: Bearer {access_token}
   ```

2. **HTTPS Only**: Tokens must only be transmitted over HTTPS

3. **Token Expiration**: Short-lived tokens (1 hour) with refresh tokens

4. **Scope Validation**: API validates token scope includes "myMeetingsAPI"

5. **Deny by Default**: If no permission check, access denied

### Testing

Easy to test with specific permissions:

```csharp
[Test]
public async Task User_With_GetMeetingDetails_Permission_Can_Access_Meeting()
{
    // Arrange
    var user = CreateUserWithPermissions(MeetingsPermissions.GetMeetingDetails);
    AuthenticateAs(user);
    
    // Act
    var response = await GetAsync($"/api/meetings/{meetingId}");
    
    // Assert
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
}

[Test]
public async Task User_Without_Permission_Cannot_Access_Meeting()
{
    // Arrange
    var user = CreateUserWithoutPermissions();
    AuthenticateAs(user);
    
    // Act
    var response = await GetAsync($"/api/meetings/{meetingId}");
    
    // Assert
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
}

## References

- [IdentityServer4 Documentation](http://docs.identityserver.io)
- [OAuth 2.0 Resource Owner Password Grant](https://www.oauth.com/oauth2-servers/access-tokens/password-grant/)
- Section 3.9 "Security" in project README
