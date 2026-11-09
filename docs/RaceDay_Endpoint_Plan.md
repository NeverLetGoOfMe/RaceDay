# RaceDay API Endpoint Plan

This table lists every endpoint the RaceDay API exposes. It was first planned in Part 1 and revised for Part 2 (see "Changes since Part 1" at the bottom).
Authentication uses server-side sessions: logging in stores the user's ID and role in the session, and protected endpoints check them.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new account as an Organiser or Participant. | None | `{ fullName, email, password, contactNumber, role }` | 201 Created – user (no password) / 400 Bad Request – invalid data or role / 409 Conflict – email already registered |
| POST | /api/auth/login | Checks credentials and starts a session holding the user ID and role. | None | `{ email, password }` | 200 OK – user ID and role / 401 Unauthorized – wrong email or password |
| POST | /api/auth/logout | Ends the current session. | Any | None | 200 OK / 401 Unauthorized |
| GET | /api/users/me | Returns the logged-in user's profile. | Any | None | 200 OK – profile / 401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's own profile. | Any | `{ fullName, contactNumber }` | 200 OK – updated profile / 400 / 401 |
| GET | /api/events | Lists all events so users can browse them. | None | None | 200 OK – array of events |
| GET | /api/events/{id} | Returns the details of one event. | None | None | 200 OK – event / 404 Not Found |
| POST | /api/events | Creates a new event owned by the logged-in organiser. | Organiser | `{ eventName, eventDate, location, description, routeInfo, distance, eventType }` | 201 Created – event / 400 / 401 / 403 Forbidden |
| PUT | /api/events/{id} | Edits an event the organiser owns. | Organiser | Same as create | 200 OK – event / 403 – not the owner / 404 |
| DELETE | /api/events/{id} | Deletes an event the organiser owns. | Organiser | None | 204 No Content / 403 / 404 |
| GET | /api/events/{eventId}/categories | Lists the categories available for an event. | None | None | 200 OK – array of categories / 404 – event not found |
| POST | /api/events/{eventId}/categories | Adds a category to an event the organiser owns. | Organiser | `{ categoryName, maxParticipants, entryFee }` | 201 Created – category / 403 / 404 |
| PUT | /api/categories/{id} | Edits a category on one of the organiser's events. | Organiser | Same as create | 200 OK – category / 403 / 404 |
| DELETE | /api/categories/{id} | Removes a category from one of the organiser's events. | Organiser | None | 204 No Content / 403 / 404 |
| POST | /api/enrolments | Enters the logged-in participant into an event by selecting a category. | Participant | `{ categoryId }` | 201 Created – enrolment / 404 – category not found / 409 – already enrolled or category full |
| GET | /api/enrolments/me | Lists the participant's own enrolments. | Participant | None | 200 OK – array of enrolments |
| GET | /api/events/{eventId}/enrolments | Lets an organiser see every enrolment for one of their events. | Organiser | None | 200 OK – array of enrolments / 403 / 404 |
| POST | /api/results | Captures a finish time and position for an enrolment. | Organiser | `{ enrolmentId, finishTime, position }` | 201 Created – result / 403 – not the organiser of that event / 404 / 409 – result already captured |
| GET | /api/results/me | Lets a participant view their own results. | Participant | None | 200 OK – array of results |
| GET | /api/categories/{categoryId}/results | Shows the results for a category, ordered by position. | None | None | 200 OK – array of results / 404 |

## Added in Part 3

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/mine | Lists the organiser's own events with the number of enrolments for each (dashboard). | Organiser | None | 200 OK – array of event summaries |
| POST | /api/events/{eventId}/banner | Uploads an event banner image to Azure Blob Storage. | Organiser | multipart form: `file` (JPEG, PNG or WebP, up to 5 MB) | 200 OK – image name / 400 / 403 / 404 / 503 – storage unavailable |
| POST | /api/users/me/picture | Uploads the logged-in user's profile picture to Azure Blob Storage. | Any | multipart form: `file` | 200 OK – image name / 400 / 503 |
| GET | /api/images/{name} | Returns a stored image so the website can display it (the container stays private). | None | None | 200 OK – image / 404 Not Found |
| PUT | /api/enrolments/{id}/status | Lets an organiser change an enrolment's status. | Organiser | `{ status }` (Pending, Confirmed or Cancelled) | 200 OK – enrolment / 403 / 404 |

`GET /api/events` also accepts the optional query options `type`, `search` and `from`, and results now include `eventDate` and `totalFinishers`.

## Changes since Part 1

- **Sessions instead of JWT.** The Part 2 brief asks for session-based authentication, so login now stores the user ID and role in a server-side session instead of returning a token. I added `POST /api/auth/logout` because a session needs a way to end.
- **Events now hold `distance` and `eventType`** (Run, Walk or Cycle), as the Part 2 brief requires. `distance` was removed from categories, which now describe groups such as "Under 20" or "Senior".
- **Added `GET /api/results/me`** so participants can view their own results.
- **Part 3:** events and users now store the name of an uploaded image (`BannerImage`, `ProfilePicture`). Results are published as soon as an organiser captures them.
