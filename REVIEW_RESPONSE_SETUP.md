# Review Response Management System - Setup Guide

## Overview

The Review Response Management System has been implemented with the following features:

### Features Added

1. **Response Fields in ProductReview Model**
   - `Response` - The business response text
   - `ResponseDate` - When the response was created
   - `ResponseUserId` - Who created the response

2. **Email Notification System** 
   - New review notifications to admin
   - Response notifications to customers
   - Microsoft Graph API integration

3. **Response Management UI**
   - ReviewResponseDialog component for adding/editing responses
   - Response display in review lists (both card and grid view)
   - Response guidelines for staff

4. **Automated Workflows**
   - Email notifications for new reviews
   - Email notifications when responses are added
   - Background processing to prevent UI blocking

## Microsoft Graph API Setup

### 1. Azure App Registration

Create an Azure App Registration with the following permissions:
- `Mail.Send` (Application permission)
- `User.Read` (Application permission if using delegated)

### 2. Configuration

Add the following to your `appsettings.json` or User Secrets:

```json
{
  "MicrosoftGraph": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret", 
    "TenantId": "your-tenant-id",
    "SenderEmail": "notifications@yourdomain.com"
  },
  "EmailNotifications": {
    "AdminEmail": "admin@yourdomain.com",
    "Enabled": true
  }
}
```

### 3. User Secrets (Recommended)

```bash
dotnet user-secrets set "MicrosoftGraph:ClientId" "your-client-id" --project AppProduct
dotnet user-secrets set "MicrosoftGraph:ClientSecret" "your-client-secret" --project AppProduct
dotnet user-secrets set "MicrosoftGraph:TenantId" "your-tenant-id" --project AppProduct
dotnet user-secrets set "MicrosoftGraph:SenderEmail" "notifications@yourdomain.com" --project AppProduct
dotnet user-secrets set "EmailNotifications:AdminEmail" "admin@yourdomain.com" --project AppProduct
```

## Usage

### For Business Users

1. **View Reviews**: Navigate to Product Reviews page
2. **Respond to Review**: Click the reply button next to any review
3. **Response Dialog**: 
   - Shows original review context
   - Provides response guidelines
   - Validates response content
   - Shows existing response if available

### Response Guidelines Included

- Thank the customer for their feedback
- Address specific concerns or questions  
- Provide helpful information or solutions
- Maintain a professional and friendly tone
- Invite further communication if needed

### Email Notifications

**New Review Notifications (to Admin):**
- Triggered when customers submit new reviews
- Contains review details and rating
- Includes direct link to respond (future enhancement)

**Response Notifications (to Customer):**
- Sent when business responds to their review
- Shows original review context
- Displays the business response
- Professional branded email template

## Technical Implementation

### Database Migration

Applied migration `AddReviewResponseFields` adds:
- Response (nvarchar)
- ResponseDate (datetime2) 
- ResponseUserId (bigint)

### Service Integration

- `EmailNotificationService` handles all email operations
- Graceful fallback when Graph API is not configured
- Background task processing prevents UI blocking
- Error logging without failing main operations

### UI Components

- `ReviewResponseDialog.razor` - Main response interface
- Updated `ListProductReview.razor` - Shows responses in UI
- Response display in both card and grid views
- Visual indicators for responded/unresponded reviews

## Benefits for Small Businesses

1. **Customer Engagement** - Direct response capability builds trust
2. **Reputation Management** - Professional responses to negative reviews
3. **Customer Retention** - Shows business cares about feedback
4. **Conversion Boost** - Studies show responses increase conversion rates by 10-30%
5. **Review Insights** - Track which reviews need responses

## Future Enhancements

- Response templates for common scenarios
- Auto-response suggestions using AI
- Review sentiment analysis
- Response performance analytics
- Bulk response operations
- Mobile push notifications