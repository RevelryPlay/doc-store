namespace DocStore.Api.Seeding;

/// <summary>A multi-page sample so page counts and previews look realistic alongside the short documents.</summary>
internal static class HandbookContent
{
    public static readonly string[] Paragraphs =
    [
        "1. Welcome",
        "This handbook describes how we work together, what you can expect from the company, and what the company expects from you. It is a living document. When a policy changes, the updated handbook is published to the document store and every employee is notified. If anything here is unclear, ask your manager or contact the people operations team directly.",
        "We are a small team, and most of what makes the team work is not written down in any policy. It is the habit of asking questions early, sharing context generously, and assuming good intent when a message reads more sharply than its author meant. The policies below exist to make the common cases predictable so that judgment can be saved for the uncommon ones.",
        "2. Working hours and location",
        "Core collaboration happens in person two days each week. On those days we hold planning sessions, pair on difficult problems, and review work that benefits from a whiteboard. On the remaining days you may work from home or from the office, and you may arrange your hours around personal commitments as long as you remain reachable during the shared midday window.",
        "Please keep your calendar current so teammates can find time with you. If you will be unavailable for more than half a day during the normal work week, mark it on the shared calendar and let your manager know. Flexibility works because everyone can see where everyone else is.",
        "3. Paid time off and holidays",
        "Full-time employees accrue paid vacation from their first day. Requests for a week or more should be submitted at least two weeks in advance so that coverage can be planned. Shorter requests can be made with reasonable notice. Sick days are separate from vacation and do not require advance notice; simply let your manager know as early as you are able.",
        "The company observes the standard set of federal holidays. When a holiday falls on a weekend, the nearest weekday is observed instead. The holiday schedule for the coming year is published every December alongside the open enrollment materials.",
        "4. Benefits",
        "Medical and dental coverage is fully paid for employees and begins on the first of the month following your start date. The company contributes three percent of salary to each employee's retirement account regardless of the employee's own contribution. Continuing education is reimbursed for courses, conferences, and certifications that relate to your role, subject to annual limits described in the benefits overview.",
        "Open enrollment runs each November. During that window you may change your elections, add or remove dependents, and review any changes to the plans for the coming year. Qualifying life events such as marriage or the birth of a child allow changes outside the enrollment window.",
        "5. Equipment and security",
        "Every employee receives a laptop and the peripherals needed to work comfortably at home and in the office. Company equipment should be used primarily for work, kept up to date, and protected with full-disk encryption and a strong password. Report a lost or stolen device immediately so that access can be revoked.",
        "Employee records, client documents, and anything containing personal information must stay inside approved systems. Do not copy sensitive documents to personal storage, and do not share them through links that bypass the access controls of the document store. When in doubt, share with specific people rather than with anyone who has a link.",
        "6. Code review and quality",
        "All changes to production systems go through code review. Reviewers look for correctness first, then clarity, then consistency with the surrounding code. Small, focused changes are easier to review and safer to deploy. Automated builds and tests must pass before a change is merged, and the author remains responsible for a change after it ships.",
        "When you use AI coding tools, you own the result exactly as you would own code you typed yourself. Review generated code line by line, run the tests, and never paste customer data, credentials, or employee records into a tool that has not been approved for that purpose.",
        "7. On-call and production support",
        "Engineers share a weekly on-call rotation. The on-call engineer triages incoming issues, communicates with affected clients, and escalates when a problem needs more hands. Time spent responding outside normal hours may be taken back later in the week. After any significant incident we write a short, blameless review focused on what we will change.",
        "8. Questions and updates",
        "Policies change as the company grows. Proposed changes are shared for comment before they take effect, and the final version is posted here. If you believe a policy is unclear, outdated, or unfair, raise it with your manager or the people operations team. Good policies come from people noticing where the current ones fall short.",
    ];
}
