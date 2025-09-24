# FajrSquad Notifications System - Runbook

## Overview

This document provides a comprehensive guide to the FajrSquad notification system, including setup, configuration, monitoring, and troubleshooting.

## System Architecture

The notification system consists of several key components:

- **FCM Notification Sender**: Handles Firebase Cloud Messaging integration
- **Message Builder**: Creates localized notification messages
- **Notification Scheduler**: Manages scheduled notifications
- **Privacy Service**: Enforces user preferences and rate limiting
- **Metrics Service**: Tracks notification performance and logs
- **Quartz Jobs**: Automated notification processing

## Setup Instructions

### 1. Firebase Configuration

1. Create a Firebase project at https://console.firebase.google.com
2. Enable Cloud Messaging in your Firebase project
3. Download the service account key JSON file
4. Set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable to point to the JSON file

```bash
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/your/firebase-service-account.json"
```

### 2. Database Migration

Run the notification system migration:

```bash
cd FajrSquad.API
dotnet ef database update --project ../FajrSquad.Infrastructure --startup-project .
```

### 3. Configuration

Copy `appsettings.sample.json` to `appsettings.json` and update the following:

- Database connection string
- Firebase project ID
- JWT secret key
- Admin user IDs

### 4. Environment Variables

Set the following environment variables:

```bash
# Firebase
GOOGLE_APPLICATION_CREDENTIALS=/path/to/firebase-service-account.json

# Database
ConnectionStrings__DefaultConnection="your-connection-string"

# Optional: CORS origins
CORS_ORIGINS="http://localhost:8100,https://yourdomain.com"
```

## API Endpoints

### Device Registration

**POST** `/api/notifications/devices/register`

Register a device token for push notifications.

```json
{
  "token": "fcm-device-token",
  "platform": "Android",
  "language": "it",
  "timeZone": "Europe/Rome",
  "appVersion": "1.0.0"
}
```

### Notification Preferences

**GET** `/api/notifications/preferences`
**PUT** `/api/notifications/preferences`

Manage user notification preferences.

```json
{
  "morning": true,
  "evening": true,
  "fajrMissed": true,
  "escalation": true,
  "hadithDaily": true,
  "motivationDaily": true,
  "eventsNew": true,
  "eventsReminder": true,
  "quietHoursStart": "22:00:00",
  "quietHoursEnd": "06:00:00"
}
```

### Debug Endpoints

**POST** `/api/notifications/debug/send`

Send a test notification (admin only).

```json
{
  "title": "Test Notification",
  "body": "This is a test message",
  "data": {
    "action": "open_app",
    "screen": "home"
  },
  "priority": "Normal"
}
```

### Health Check

**GET** `/health/notifications`

Check notification system health and metrics.

## Notification Types

### 1. Morning Reminders
- **Trigger**: Every 10 minutes, checks for users in morning time window (5:00-8:00 AM local)
- **Content**: Personalized dua and reminders
- **Priority**: Normal

### 2. Evening Reminders
- **Trigger**: Every 10 minutes, checks for users at evening time (21:30 local)
- **Content**: Wudu and adhkar reminders
- **Priority**: Normal

### 3. Fajr Missed Notifications
- **Trigger**: Daily at 08:30 local time
- **Content**: Motivational messages for missed Fajr
- **Priority**: High

### 4. Escalation Reminders
- **Trigger**: Daily at 11:30 local time
- **Content**: Additional motivation for users who haven't checked in
- **Priority**: High

### 5. Daily Hadith
- **Trigger**: Daily at 08:00 local time
- **Content**: Random hadith in user's language
- **Priority**: Normal

### 6. Daily Motivation
- **Trigger**: Daily at 08:05 local time
- **Content**: Motivational message
- **Priority**: Normal

### 7. Event Notifications
- **Immediate**: Sent when new events are created
- **Reminders**: 24 hours and 2 hours before event start
- **Priority**: Normal

### 8. Admin Alerts
- **Trigger**: When users miss Fajr for 3+ consecutive days
- **Content**: User details and missed days count
- **Priority**: High

## Privacy Controls

### User Preferences
Users can control which notifications they receive through the preferences API.

### Quiet Hours
Notifications are blocked during user-defined quiet hours (default: 22:00-06:00).

### Rate Limiting
Maximum 10 notifications per user per day (configurable).

### Urgent Notifications
Some notifications (admin alerts, Fajr missed, escalation) bypass quiet hours and rate limits.

## Monitoring

### Health Check
Monitor system health at `/health/notifications`:

```json
{
  "status": "healthy",
  "last24Hours": {
    "totalSent": 1250,
    "totalFailed": 15,
    "successRate": 98.8,
    "sentByType": {
      "MorningReminder": 450,
      "EveningReminder": 380,
      "DailyHadith": 200,
      "DailyMotivation": 220
    }
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Logs
All notifications are logged in the `NotificationLogs` table with:
- User ID
- Notification type
- Result (Sent/Failed)
- Error details
- Timestamp
- Retry count

### Metrics
The system tracks:
- Success/failure rates
- Notifications by type
- Notifications by hour
- Top errors
- Average processing time

## Troubleshooting

### Common Issues

#### 1. Firebase Authentication Errors
```
Error: Your default credentials were not found
```
**Solution**: Set `GOOGLE_APPLICATION_CREDENTIALS` environment variable.

#### 2. Device Token Not Registered
```
Error: No active device tokens found
```
**Solution**: Ensure device registration endpoint is called after user login.

#### 3. Notifications Not Sending
**Check**:
- Firebase project configuration
- Device token validity
- User notification preferences
- Quiet hours settings
- Rate limiting

#### 4. High Failure Rate
**Check**:
- Firebase quota limits
- Network connectivity
- Invalid device tokens (auto-cleanup enabled)

### Debugging Steps

1. Check application logs for error details
2. Verify Firebase configuration
3. Test with debug notification endpoint
4. Check notification logs in database
5. Monitor health check endpoint

### Database Queries

#### Check notification logs
```sql
SELECT Type, Result, COUNT(*) as Count, DATE(SentAt) as Date
FROM NotificationLogs 
WHERE SentAt >= NOW() - INTERVAL '7 days'
GROUP BY Type, Result, DATE(SentAt)
ORDER BY Date DESC, Type;
```

#### Check scheduled notifications
```sql
SELECT Type, Status, COUNT(*) as Count
FROM ScheduledNotifications 
WHERE ExecuteAt >= NOW() - INTERVAL '1 day'
GROUP BY Type, Status;
```

#### Check user preferences
```sql
SELECT 
  COUNT(*) as TotalUsers,
  SUM(CASE WHEN Morning = true THEN 1 ELSE 0 END) as MorningEnabled,
  SUM(CASE WHEN Evening = true THEN 1 ELSE 0 END) as EveningEnabled,
  SUM(CASE WHEN FajrMissed = true THEN 1 ELSE 0 END) as FajrMissedEnabled
FROM UserNotificationPreferences;
```

## Maintenance

### Daily Tasks
- Monitor health check endpoint
- Review notification logs for errors
- Check Firebase quota usage

### Weekly Tasks
- Review notification metrics
- Clean up old logs (automated)
- Update notification templates if needed

### Monthly Tasks
- Analyze user engagement metrics
- Review and update admin user list
- Performance optimization review

## Security Considerations

1. **Firebase Security**: Keep service account keys secure
2. **Rate Limiting**: Prevents notification spam
3. **User Privacy**: Respect quiet hours and preferences
4. **Admin Access**: Limit debug endpoints to admin users
5. **Data Retention**: Automatic cleanup of old logs

## Performance Optimization

1. **Batch Processing**: Notifications are processed in batches
2. **Retry Logic**: Exponential backoff for failed notifications
3. **Database Indexing**: Optimized queries for notification lookups
4. **Caching**: User preferences cached for performance
5. **Async Processing**: All notification operations are asynchronous

## Scaling Considerations

1. **Database**: Consider read replicas for notification queries
2. **Firebase**: Monitor quota limits and upgrade as needed
3. **Jobs**: Quartz jobs can be scaled horizontally
4. **Monitoring**: Implement alerting for high failure rates

## Support

For issues or questions:
1. Check this runbook first
2. Review application logs
3. Check Firebase console for quota/errors
4. Contact development team with specific error details

---

**Last Updated**: January 2024
**Version**: 1.0
