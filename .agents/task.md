# Task List: UC19 & UC20 - Process Incident Reports & Maintenance Tickets

- `[ ]` Map the "Sự cố & Bảo trì" sidebar menu link in `_Layout.cshtml` to `/TechnicianIncidents/Index`.
- `[ ]` Create view models for incident processing:
  - `TechnicianIncidentListViewModel.cs` for list filtering.
  - `MaintenanceTicketViewModel.cs` for sending equipment to external maintenance.
- `[ ]` Create `TechnicianIncidentsController.cs` with the following actions:
  - `Index` (GET) - lists all reports with search and filter capability.
  - `Details` (GET) - view specific report, showing action buttons based on status.
  - `Accept` (POST) - accept a pending incident.
  - `Resolve` (POST) - resolve an in-progress incident.
  - `CreateTicket` (GET & POST) - create external repair ticket.
  - `CompleteTicket` (POST) - complete external repair ticket, returning equipment to service.
- `[ ]` Create views under `Views/TechnicianIncidents/`:
  - `Index.cshtml` - table list with badges, search, and action filters.
  - `Details.cshtml` - details view with contextual action panels (Resolve form, Accept button, Complete Maintenance form).
  - `CreateTicket.cshtml` - form to create external repair ticket.
- `[ ]` Verify build compiles cleanly.
- `[ ]` Walkthrough verification.
