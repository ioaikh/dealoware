namespace Dealoware.Api.Admin;

/// <summary>
/// Sentences the admin delete API answers with (A7 Step 10). The delete
/// dialog (a later piece) shows them. None repeats a stored value.
/// <para>
/// The exact wording is still open (test design OQ6): no document gives it,
/// so these are new sentences, kept in one place to be replaced.
/// </para>
/// </summary>
public static class AdminDeleteCopy
{
    /// <summary>The <c>error</c> of a 409 whose confirm token cannot be used.</summary>
    public const string TokenInvalidError = "ConfirmTokenInvalid";

    /// <summary>The <c>error</c> of a 409 for an artifact a negotiation still refers to.</summary>
    public const string BlockedError = "Blocked";

    /// <summary>
    /// The confirm token is unknown, used, run out, made in another session or
    /// for another record. One sentence for all of them.
    /// </summary>
    public const string TokenInvalid =
        "This delete confirmation is no longer valid. Nothing was deleted. Open the delete again.";

    /// <summary>
    /// The record, or a row that would go with it, changed after the delete
    /// was opened (a party acted, or another admin write landed).
    /// </summary>
    public const string Changed =
        "This record or something deleted with it changed after you opened the delete. Nothing was deleted. Open the delete again.";

    /// <summary>Spec §11.2: an artifact is not deleted while a negotiation that is not deleted refers to it.</summary>
    public const string ArtifactBlocked =
        "This artifact cannot be deleted while negotiations refer to it. Delete those negotiations first.";
}
