# MeetingRoomReservation

A booking system for a limited set of meeting rooms where concurrent requests for the same time
slot must never result in a double-booking, and booking status changes are reflected to all
viewers in real time. See `CLAUDE.md` for the full architecture and domain rules.

## Seeded test accounts

On first run, `DatabaseSeeder` (`MeetingRoomReservation.Infrastructure/Data/Seeders`) creates the
`Admin`/`User` roles, two demo resources, and these two accounts:

| Role  | Email                     | Password      |
|-------|---------------------------|---------------|
| Admin | admin@meetingrooms.local  | Admin#12345   |
| User  | user@meetingrooms.local   | User#12345    |

These are local-dev-only demo credentials seeded by `DatabaseSeeder` - never reuse them outside
a local/dev environment.