namespace Dealoware.Api.Admin;

/// <summary>
/// Sentences the admin edit API answers with (A7 Step 9). The edit page (a
/// later piece) shows them beside the field named by the key. Each one states
/// a rule; none repeats a value that was sent or that is stored.
/// <para>
/// The exact wording is still open (test design OQ6): no document gives it,
/// so these are new sentences, kept in one place to be replaced.
/// </para>
/// </summary>
public static class AdminEditCopy
{
    /// <summary>Key for a message about the request as a whole, not one field.</summary>
    public const string FormKey = "form";

    public const string NoChanges = "No changes.";

    /// <summary>The 409: the record is not as it was when this change was prepared.</summary>
    public const string Conflict = "This record changed after you opened it. Reload it and try again.";

    public const string DisplayNameTooLong = "Enter a display name of at most 256 characters.";
    public const string ActiveNotChosen = "Choose Active or Suspended.";

    public const string NameRequired = "Name is required.";
    public const string NameTooLong = "Enter a name of at most 4096 characters.";
    public const string DescriptionTooLong = "Enter a description of at most 4096 characters.";
    public const string OwnerNotChosen = "Choose an existing participant.";

    public const string EndsAtInvalid = "Enter a valid end time.";
    public const string EndsAtNotAfterStart = "Ends at must be after Starts at.";
}
