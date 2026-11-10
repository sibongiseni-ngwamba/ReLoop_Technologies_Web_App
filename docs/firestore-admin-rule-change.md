# Enable administrator-created reward claims

The mobile app's existing `redemptionLogs` rule only lets a signed-in user create a claim for their own UID. The web admin form can list and update claims already, but creating one for another account requires an explicit admin exception. The mobile rules do not yet define a `contactMessages` collection, so add a constrained public-create rule for the site's Contact form as well.

In Firebase Console → **Firestore Database → Rules**, find the `match /redemptionLogs/{redemptionId}` block and replace only its `allow create` line with:

```text
allow create: if isAdmin() || (isSignedIn() && request.resource.data.userId == request.auth.uid);
```

Keep the block's existing read/update/delete rules, review the complete rules, then publish them in the Firebase Console. No CLI is needed. This grants the operation only to accounts whose existing `users/{uid}.role` is `admin`; do not weaken the role checks.

Also add this match block inside `match /databases/{database}/documents` to support the Contact form. It validates message lengths and field names, allows anyone to submit a message, and restricts reading or changing messages to admins:

```text
match /contactMessages/{messageId} {
  allow create: if request.resource.data.keys().hasOnly([
      'contactMessageId', 'userId', 'fullName', 'email', 'subject', 'message', 'status', 'createdAt'
    ])
    && request.resource.data.fullName is string
    && request.resource.data.fullName.size() > 0
    && request.resource.data.fullName.size() <= 120
    && request.resource.data.email is string
    && request.resource.data.email.size() <= 254
    && request.resource.data.subject is string
    && request.resource.data.subject.size() > 0
    && request.resource.data.subject.size() <= 160
    && request.resource.data.message is string
    && request.resource.data.message.size() >= 10
    && request.resource.data.message.size() <= 4000
    && request.resource.data.status == 'New';
  allow read, update, delete: if isAdmin();
}
```

Review the full rules in Firebase Console and publish them there. The repository and website do not run a rules or database deployment command.
