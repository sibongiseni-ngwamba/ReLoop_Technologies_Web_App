# ReLoop Technologies Web App

ReLoop is an ASP.NET Core Razor Pages web client for the same Firebase project used by the Android app. Browser sign-in uses Firebase Authentication, and application data is read from and written to the shared Cloud Firestore collections through the Firebase JavaScript SDK. The web client does not need Firebase CLI, Google CLI, a service-account key, or a deployed REST API.

## Local setup

1. In Firebase Console, open project `relooptechnologies-2eda2`, choose **Add app → Web**, and register this site. Copy the web app configuration values. This is a browser app configuration; never use a service account or Admin SDK key here.
2. In Firebase Console → **Authentication → Sign-in method**, enable **Email/Password**. In **Authentication → Settings → Authorized domains**, add the host you use locally if it is not already listed.
3. In Firestore, keep the rules shared with the Android app. The current web workflows use the `users`, `pickups`, `rewards`, `rewardItems`, `redemptionLogs`, `pointsTransactions`, `wasteLogs`, and `contactMessages` collections. Admin access comes from the signed-in user’s `users/{uid}.role == "admin"` profile and is enforced by Firestore rules. Apply the two Console-only rule additions in [`docs/firestore-admin-rule-change.md`](docs/firestore-admin-rule-change.md) to allow admins to create claims for other users and to safely accept contact messages.
4. Put the web app’s `apiKey`, `authDomain`, `projectId`, and `appId` in `appsettings.Development.json` under `Firebase`. The API key and app ID are public browser identifiers; do not put a service-account key in this file. Keep `Firebase:ProjectId` set to the shared mobile project.
5. Start the app with `dotnet run` and open the local address printed by ASP.NET Core.

Example configuration shape:

```json
{
  "Firebase": {
    "ApiKey": "your-web-api-key",
    "AuthDomain": "your-project.firebaseapp.com",
    "ProjectId": "relooptechnologies-2eda2",
    "AppId": "1:project-number:web:web-app-id"
  }
}
```

The default repository settings intentionally leave the web API key and app ID empty until a Web app is registered in Firebase Console. Firebase configuration is injected into the page at request time; no credentials or private keys are compiled into the JavaScript file.

## Web features

- Email/password registration and sign-in through Firebase Authentication.
- User profile, pickup schedule/history, dashboard metrics, demo scan records, and contact messages in Firestore.
- Role-checked admin console for user roles/activation, pickup status, rewards catalogue, redemption claim status, and scan records.
- Existing ReLoop landing, about, contact, and account pages.

The waste classifier remains a filename-based demonstration; it is not an image recognition model. Firebase access requires the Firestore rules and Authentication settings in the shared project to permit the signed-in role and domain.

## Admin setup and controls

Create the admin account in the web or Android app first. In Firebase Console → **Firestore Database → Data**, open `users/{that account's Firebase Auth UID}` and set its `role` field to the string `admin`. Admin screens are hidden behind a profile-role check, and Firestore rules remain the actual authorization boundary. Never make the admin role self-editable for ordinary users.

The mobile Firestore rules currently allow signed-in admins to manage users, pickups, reward items, and redemption logs. They also allow members to change their own reward balances and create their own points transactions; this is a known mobile-side security limitation for reward value. Do not treat client-side reward points as monetary value until reward mutations move behind trusted server-side validation.

## Tests

Run the .NET rendered-page and API integration tests with `dotnet test`, and run the browser-domain unit tests with `node --test tests/javascript/*.test.cjs`. These tests do not need Firebase credentials. The admin page smoke check covers its management sections; live Firebase access still requires manual verification with a configured Firebase project and test accounts.

The GitHub Actions workflow in `.github/workflows/dotnet-ci.yml` only builds and runs tests. It does not deploy the web app or change Firebase data. There are no Firebase, Google Cloud, or Supabase deployment workflows in this repository.

## Design references

The original UI wireframes are under [`docs/wireframes`](docs/wireframes).

## Application Screenshots

| | |
|---|---|
| ![ReLoop landing page](assets/Screenshot%202026-11-10%20192511.png)<br>**Landing page** — Overview of ReLoop and its recycling features. | ![About ReLoop](assets/Screenshot%202026-11-10%20192534.png)<br>**About page** — Mission, workflow, and impact model. |
| ![Contact page](assets/Screenshot%202026-11-10%20192543.png)<br>**Contact form** — Support contact details and message form. | ![How ReLoop works](assets/Screenshot%202026-11-10%20192557.png)<br>**How it works** — Three steps from scanning to rewards. |
| ![Login page](assets/Screenshot%202026-11-10%20192613.png)<br>**Login form** — Email and password sign-in. | ![Login page with Google option](assets/Screenshot%202026-11-10%20192623.png)<br>**Login options** — Google sign-in alongside the login form. |
| ![Sign-up page](assets/Screenshot%202026-11-10%20192635.png)<br>**Sign-up form** — Create an account with name, email, and password. | ![Sign-up page with Google option](assets/Screenshot%202026-11-10%20192647.png)<br>**Sign-up options** — Google sign-in alongside account creation. |
| ![ReLoop profile](assets/Screenshot%202026-11-10%20192725.png)<br>**Profile** — Account details and recycling summary. | ![ReLoop dashboard](assets/Screenshot%202026-11-10%20192747.png)<br>**Dashboard** — Reward points, scan totals, and pickup activity. |
| ![Dashboard activity](assets/Screenshot%202026-11-10%20192753.png)<br>**Dashboard activity** — Recent activity and upcoming pickups. | ![Waste scanning page](assets/Screenshot%202026-11-10%20192802.png)<br>**Waste scanning** — Upload an item image for classification. |
| ![Waste scan guidance](assets/Screenshot%202026-11-10%20192809.png)<br>**Scan guidance** — Tips and reward information for scans. | ![Schedule pickup form](assets/Screenshot%202026-11-10%20192818.png)<br>**Schedule pickup** — Choose an address, date, time, and waste type. |
| ![Pickup form details](assets/Screenshot%202026-11-10%20192825.png)<br>**Pickup notes** — Add collection instructions and submit a request. | ![Pickup ledger](assets/Screenshot%202026-11-10%20192835.png)<br>**Pickup ledger** — Filter and review collection history. |
| ![Pickup ledger record](assets/Screenshot%202026-11-10%20192843.png)<br>**Pickup record** — Example scheduled collection in the ledger. | |

## Demo Video

[Watch or download the ReLoop demo video](assets/Demo%20Video.mp4).
