# JWT Authentication & Authorization Testing Guide

## Overview

This guide demonstrates how to test the JWT authentication and authorization features implemented in the YARP proxy.

## Features Implemented

### 1. JWT Validation
- **Issuer validation**: Ensures token comes from trusted issuer
- **Audience validation**: Verifies token is intended for this API
- **Lifetime validation**: Checks token expiration
- **Signature validation**: Validates token integrity

### 2. Authorization Policies

| Policy Name | Requirements |
|------------|--------------|
| `AdminOnly` | Authenticated + Admin role |
| `UserPolicy` | Authenticated + User or Admin role |
| `RequireApiScope` | Authenticated + scope: `api.read` or `api.write` |
| `AdvancedPolicy` | Admin role + `api.write` scope + write/delete permissions |

### 3. Route Protection

| Route | Path | Protection | Token Forwarding |
|-------|------|-----------|------------------|
| Anonymous | `/Cities` | None | No (adds X-Forwarded-User: anonymous) |
| Anonymous | `/scalar/*` | None | No |
| User Protected | `/user/*` | UserPolicy | Yes + adds X-User-Id, X-User-Roles headers |
| Admin Protected | `/admin/*` | AdminOnly | Yes + forwards original Authorization header |
| Scope Protected | `/api/*` | RequireApiScope | Yes |

### 4. Token Forwarding Rules

**Transforms Applied:**
- Forward `Authorization` header to downstream services
- Add `X-User-Id` header from JWT `sub` claim
- Add `X-User-Roles` header from JWT `role` claim
- Add `X-Forwarded-*` headers for proxy information

## Testing Instructions

### Step 1: Start All Services

**Terminal 1 - Web API:**
```bash
cd "src/WebApi"
dotnet run --urls http://localhost:5002
```

**Terminal 2 - Blazor App:**
```bash
cd "src/WebApp"
dotnet run --urls http://localhost:5001
```

**Terminal 3 - Proxy (requires sudo for port 80):**
```bash
cd "src/RevereseProxy"
sudo dotnet run
```

### Step 2: Test Anonymous Routes

These routes don't require authentication:

```bash
# Access Cities endpoint (anonymous)
curl http://api.localhost/Cities

# Access Scalar API documentation (anonymous)
curl http://api.localhost/scalar/v1

# Access Blazor app (anonymous)
curl http://webapp.localhost
```

**Expected**: All should work without authentication (200 OK)

### Step 3: Generate JWT Tokens

The proxy has a test endpoint to generate tokens:

**Generate User token:**
```bash
curl http://localhost/auth/token?role=User&scope=api.read
```

**Generate Admin token:**
```bash
curl http://localhost/auth/token?role=Admin&scope=api.write
```

**Save the token:**
```bash
# Example response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": "2026-01-18T15:30:00Z",
  "role": "Admin",
  "scope": "api.write",
  "usage": "Use: Authorization: Bearer <token>"
}
```

### Step 4: Test Protected Routes WITHOUT Token

These should fail with 401 Unauthorized:

```bash
# Try accessing admin route without token
curl -v http://api.localhost/admin/users

# Try accessing user route without token
curl -v http://api.localhost/user/profile

# Try accessing API route without token
curl -v http://api.localhost/api/data
```

**Expected**: 401 Unauthorized

### Step 5: Test with User Token

**Generate User token:**
```bash
USER_TOKEN=$(curl -s http://localhost/auth/token?role=User&scope=api.read | jq -r '.token')
```

**Test User-protected route (should work):**
```bash
curl -H "Authorization: Bearer $USER_TOKEN" http://api.localhost/user/profile
```

**Test API scope-protected route (should work - has api.read):**
```bash
curl -H "Authorization: Bearer $USER_TOKEN" http://api.localhost/api/data
```

**Test Admin-only route (should fail - not Admin):**
```bash
curl -v -H "Authorization: Bearer $USER_TOKEN" http://api.localhost/admin/users
```

**Expected**: User routes work (200), Admin routes fail (403 Forbidden)

### Step 6: Test with Admin Token

**Generate Admin token:**
```bash
ADMIN_TOKEN=$(curl -s http://localhost/auth/token?role=Admin&scope=api.write | jq -r '.token')
```

**Test Admin-protected route (should work):**
```bash
curl -H "Authorization: Bearer $ADMIN_TOKEN" http://api.localhost/admin/users
```

**Test User-protected route (should work - Admin has User permissions):**
```bash
curl -H "Authorization: Bearer $ADMIN_TOKEN" http://api.localhost/user/profile
```

**Test API scope-protected route (should work - has api.write):**
```bash
curl -H "Authorization: Bearer $ADMIN_TOKEN" http://api.localhost/api/data
```

**Expected**: All routes work (200 OK)

### Step 7: Test Scope Validation

**Generate token with wrong scope:**
```bash
WRONG_SCOPE_TOKEN=$(curl -s http://localhost/auth/token?role=User&scope=wrong.scope | jq -r '.token')
```

**Test scope-protected route:**
```bash
curl -v -H "Authorization: Bearer $WRONG_SCOPE_TOKEN" http://api.localhost/api/data
```

**Expected**: 403 Forbidden (missing required scope)

### Step 8: Test Token Forwarding

Check if headers are forwarded to downstream service. You'll need to modify WebApi to log headers:

**In src/WebApi/Program.cs, add before `app.MapGet("/Cities")`:**
```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine($"Authorization: {context.Request.Headers["Authorization"]}");
    Console.WriteLine($"X-User-Id: {context.Request.Headers["X-User-Id"]}");
    Console.WriteLine($"X-User-Roles: {context.Request.Headers["X-User-Roles"]}");
    await next();
});
```

**Then test:**
```bash
curl -H "Authorization: Bearer $USER_TOKEN" http://api.localhost/user/profile
```

**Check WebApi logs** - you should see:
- `Authorization: Bearer eyJh...`
- `X-User-Id: test-user-123`
- `X-User-Roles: User`

## Testing with Postman or Thunder Client

### 1. Generate Token
- **GET** `http://localhost/auth/token?role=Admin&scope=api.write`
- Copy the `token` value from response

### 2. Test Protected Routes
- **GET** `http://api.localhost/admin/users`
- Headers:
  - `Authorization: Bearer <paste-token-here>`

### 3. Test Different Roles
- Generate tokens with different roles/scopes
- Try accessing various protected routes
- Observe 401/403 responses when not authorized

## Browser Testing with JWT Debugger

**Decode your JWT token:**
1. Visit https://jwt.io
2. Paste your token in the "Encoded" section
3. View the decoded claims:
   ```json
   {
     "sub": "test-user-123",
     "name": "Test User",
     "role": "Admin",
     "scope": "api.write",
     "permissions": "read",
     "exp": 1737214200,
     "iss": "https://your-auth-server.com",
     "aud": "yarp-proxy-api"
   }
   ```

## Configuration Reference

### JWT Settings (appsettings.json)

```json
{
  "JwtSettings": {
    "Authority": "https://your-auth-server.com",
    "Audience": "yarp-proxy-api",
    "Issuer": "https://your-auth-server.com",
    "RequireHttpsMetadata": false,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true
  }
}
```

### Authorization Policies (Program.cs)

```csharp
// Admin only
options.AddPolicy("AdminOnly", policy =>
{
    policy.RequireAuthenticatedUser();
    policy.RequireRole("Admin");
});

// User or Admin
options.AddPolicy("UserPolicy", policy =>
{
    policy.RequireAuthenticatedUser();
    policy.RequireRole("User", "Admin");
});

// Require specific scope
options.AddPolicy("RequireApiScope", policy =>
{
    policy.RequireAuthenticatedUser();
    policy.RequireClaim("scope", "api.read", "api.write");
});
```

### Route Configuration

```json
{
  "api-protected-route": {
    "ClusterId": "api-cluster",
    "Match": {
      "Hosts": ["api.localhost"],
      "Path": "/admin/{**catch-all}"
    },
    "AuthorizationPolicy": "AdminOnly",
    "Transforms": [
      {
        "RequestHeader": "Authorization",
        "Append": "{header:Authorization}"
      }
    ]
  }
}
```

## Common HTTP Status Codes

| Code | Meaning | Cause |
|------|---------|-------|
| 200 | OK | Request succeeded |
| 401 | Unauthorized | No token or invalid token |
| 403 | Forbidden | Valid token but insufficient permissions |
| 404 | Not Found | Route doesn't exist |

## Troubleshooting

### Issue: Always getting 401 Unauthorized

**Check:**
1. Token is being sent: `curl -v` to see request headers
2. Token format: Must be `Authorization: Bearer <token>`
3. Token expiration: Generate a new token
4. Issuer/Audience match configuration

### Issue: Getting 403 Forbidden with valid token

**Check:**
1. User has required role: Decode JWT at jwt.io
2. Policy requirements match token claims
3. Scope claims are present if required

### Issue: Token not forwarded to downstream

**Check:**
1. Transform configuration in route
2. Middleware order in Program.cs (UseAuthentication before UseAuthorization)
3. Downstream service logs

### Issue: Token generation endpoint not working

**Check:**
1. Accessing directly via port 80: `http://localhost/auth/token`
2. Not trying through proxy hostname

## Production Considerations

⚠️ **This is a DEMO implementation. For production:**

1. **Use a real Identity Provider** (Auth0, Azure AD, IdentityServer, Keycloak)
2. **Use asymmetric keys** (RSA) instead of symmetric keys
3. **Enable HTTPS** (`RequireHttpsMetadata: true`)
4. **Store secrets securely** (Azure Key Vault, AWS Secrets Manager)
5. **Implement refresh tokens**
6. **Add rate limiting**
7. **Enable logging and monitoring**
8. **Implement proper CORS policies**

## Testing Checklist

- [ ] Anonymous routes work without token
- [ ] Protected routes fail without token (401)
- [ ] User token accesses User routes
- [ ] User token rejected from Admin routes (403)
- [ ] Admin token accesses all routes
- [ ] Wrong scope token rejected (403)
- [ ] Expired token rejected (401)
- [ ] Authorization header forwarded to downstream
- [ ] User claims added as headers (X-User-Id, X-User-Roles)
- [ ] Token generation endpoint works

## Next Steps

1. Integrate with real identity provider
2. Add refresh token endpoint
3. Implement token revocation
4. Add audit logging
5. Set up monitoring for auth failures
6. Implement rate limiting per user
