Business Requirements Document: Subscription Retention System
1. Executive Summary
Our goal is to increase student retention by automatically notifying users before their access expires. We want to send personalized messages that remind them of their specific learning progress (e.g., C# or DevOps) to make the reminder feel helpful, not like spam.

2. User Data Requirements (What we need to know)
To make this work, the system must keep track of:

Who they are: Name and Email address.

When they lose access: The exact date their subscription ends.

What they are learning: Their primary interest (e.g., "C#" or "DevOps").

Anti-Spam Check: A record of the last time we sent them a notification so we don't email them twice in one day.

3. Functional Requirements (What the system must do)
A. The "Search" Logic
The system must automatically scan our student list every day. It should only pick students who meet both these criteria:

Their subscription expires in exactly 3 days.

They have not been sent a reminder for this specific expiration yet today.

B. Personalization Logic
The message must change based on the student's course.

C# Students: Should receive a message about their "C# journey."

DevOps Students: Should receive a message about their "cloud labs."

Goal: The student should feel like we are watching out for their specific progress.

C. The "Safe Delivery" Process (Batching)
To avoid overwhelming our email server or slowing down our database, the system must not process everyone at once.

It should process students in small groups (batches) of 2 at a time.

Once a student in a batch is emailed, the system must immediately mark them as "Notified" before moving to the next group. This ensures that if the system crashes, we don't start over and email the same people again.

4. Operational & Reliability Requirements
A. Visibility (Logging)
I need to be able to look at a "Status Log" to see exactly what the system is doing. It should look like this:

[10:00:01] Found 2 students expiring in 3 days. Starting emails...
[10:00:02] Email sent to Ivan (C# Student).
[10:00:03] Email sent to Maria (DevOps Student).

B. Graceful Shutdown
If we need to turn the server off for maintenance, the system shouldn't just "die" in the middle of an email. It should finish the current student it is processing and then stop safely.

C. "Once and Only Once" Policy (Idempotency)
If the system runs, finishes, and then accidentally runs again 5 minutes later, it should find zero students to email. It must be smart enough to know the work for today is already done.

5. Success Criteria for the Developer
Test Case 1: Create 5 students who expire in 3 days. Run the system. Verify all 5 get a custom email.

Test Case 2: Run the system again immediately. Verify zero emails are sent.

Test Case 3: Stop the program while it’s in the middle of a batch. Verify it doesn't leave the database in a "messy" state.