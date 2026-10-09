namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>ROLE_GROUP</c>: a named bundle of master roles of one company, e.g. "All Operators" = Operator RJ CL 772
/// + Operator RJ IMFL 1028. A user given the group holds all its roles; a change to the group reaches all its users at
/// once. Groups are optional: roles can still be given to a user directly.
/// </summary>
public class ROLE_GROUP
{
    private ROLE_GROUP()
    {
        GroupName = string.Empty;
    }

    public int Id { get; private set; }
    public int CompanyId { get; private set; }
    public string GroupName { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static ROLE_GROUP Create(int companyId, string groupName, string? description, DateTime now, int? createdBy)
    {
        var group = new ROLE_GROUP
        {
            CompanyId = companyId,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = createdBy,
        };

        group.Update(groupName, description, now, createdBy);
        return group;
    }

    public void Update(string groupName, string? description, DateTime now, int? updatedBy)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ArgumentException("Group name is required.", nameof(groupName));
        }

        GroupName = groupName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }
}
