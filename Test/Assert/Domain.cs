namespace Void.Platform.Test;

using Void.Platform.Domain;

public class AssertDomain
{
  //-----------------------------------------------------------------------------------------------

  public void Equal(string? expected, string? actual) { Assert.Equal(expected, actual); }
  public void Equal(long? expected, long? actual) { Assert.Equal(expected, actual); }
  public void Equal(bool expected, bool actual) { Assert.Equal(expected, actual); }
  public void Equal(Instant expected, Instant actual) { Assert.Equal(expected, actual); }
  public void Equal(Instant? expected, Instant? actual) { Assert.Equal(expected, actual); }
  public void Equal<T>(T expected, T actual) where T : Enum { Assert.Equal(expected, actual); }

  public void Equal(Account.Organization expected, Account.Organization? actual)
  {
    Assert.Present(actual);
    Assert.Domain.Equal(expected.Id, actual.Id);
    Assert.Domain.Equal(expected.Name, actual.Name);
    Assert.Domain.Equal(expected.Slug, actual.Slug);
    Assert.Domain.Equal(expected.CreatedOn, actual.CreatedOn);
    Assert.Domain.Equal(expected.UpdatedOn, actual.UpdatedOn);
  }

  public void Equal(Account.Member expected, Account.Member actual)
  {
    Assert.Domain.Equal(expected.OrganizationId, actual.OrganizationId);
    Assert.Domain.Equal(expected.UserId, actual.UserId);
    Assert.Domain.Equal(expected.CreatedOn, actual.CreatedOn);
    Assert.Domain.Equal(expected.UpdatedOn, actual.UpdatedOn);
  }

  public void Equal(Account.User expected, Account.User? actual)
  {
    Assert.Present(actual);
    Assert.Domain.Equal(expected.Id, actual.Id);
    Assert.Domain.Equal(expected.Name, actual.Name);
    Assert.Domain.Equal(expected.Email, actual.Email);
    Assert.Domain.Equal(expected.TimeZone, actual.TimeZone);
    Assert.Domain.Equal(expected.Locale, actual.Locale);
    Assert.Domain.Equal(expected.Disabled, actual.Disabled);
    Assert.Domain.Equal(expected.CreatedOn, actual.CreatedOn);
    Assert.Domain.Equal(expected.UpdatedOn, actual.UpdatedOn);
  }

  public void Equal(Account.Identity expected, Account.Identity actual)
  {
    Assert.Domain.Equal(expected.Id, actual.Id);
    Assert.Domain.Equal(expected.UserId, actual.UserId);
    Assert.Domain.Equal(expected.Provider, actual.Provider);
    Assert.Domain.Equal(expected.Identifier, actual.Identifier);
    Assert.Domain.Equal(expected.UserName, actual.UserName);
    Assert.Domain.Equal(expected.CreatedOn, actual.CreatedOn);
    Assert.Domain.Equal(expected.UpdatedOn, actual.UpdatedOn);
  }

  public void Equal(Account.AuthenticatedUser expected, Account.AuthenticatedUser actual)
  {
    Assert.Domain.Equal((Account.User) expected, (Account.User) actual);
    Assert.Domain.Equal(expected.Roles, actual.Roles);
    Assert.Domain.Equal(expected.Identities, actual.Identities);
    Assert.Domain.Equal(expected.Organizations, actual.Organizations);
    Assert.Domain.Equal(expected.AuthenticatedOn, actual.AuthenticatedOn);
  }

  public void Equal(Account.Game expected, Account.Game? actual)
  {
    Assert.Present(actual);
    Assert.Domain.Equal(expected.Id, actual.Id);
    Assert.Domain.Equal(expected.OrganizationId, actual.OrganizationId);
    Assert.Domain.Equal(expected.Purpose, actual.Purpose);
    Assert.Domain.Equal(expected.Name, actual.Name);
    Assert.Domain.Equal(expected.Slug, actual.Slug);
    Assert.Domain.Equal(expected.Description, actual.Description);
    Assert.Domain.Equal(expected.IsArchived, actual.IsArchived);
    Assert.Domain.Equal(expected.ArchivedOn, actual.ArchivedOn);
    Assert.Domain.Equal(expected.CreatedOn, actual.CreatedOn);
    Assert.Domain.Equal(expected.UpdatedOn, actual.UpdatedOn);
  }

  public void Equal(Share.Branch expected, Share.Branch actual)
  {
    Assert.Domain.Equal(expected.Id, actual.Id);
    Assert.Domain.Equal(expected.OrganizationId, actual.OrganizationId);
    Assert.Domain.Equal(expected.GameId, actual.GameId);
    Assert.Domain.Equal(expected.Slug, actual.Slug);
    Assert.Domain.Equal(expected.IsPinned, actual.IsPinned);
    Assert.Domain.Equal(expected.EncryptedPassword, actual.EncryptedPassword);
    Assert.Domain.Equal(expected.ActiveDeployId, actual.ActiveDeployId);
    Assert.Domain.Equal(expected.LatestDeployId, actual.LatestDeployId);
    Assert.Domain.Equal(expected.CreatedOn, actual.CreatedOn);
    Assert.Domain.Equal(expected.UpdatedOn, actual.UpdatedOn);
  }

  public void Equal(Share.Deploy expected, Share.Deploy? actual)
  {
    Assert.Present(actual);
    Assert.Domain.Equal(expected.Id, actual.Id);
    Assert.Domain.Equal(expected.OrganizationId, actual.OrganizationId);
    Assert.Domain.Equal(expected.GameId, actual.GameId);
    Assert.Domain.Equal(expected.BranchId, actual.BranchId);
    Assert.Domain.Equal(expected.State, actual.State);
    Assert.Domain.Equal(expected.Path, actual.Path);
    Assert.Domain.Equal(expected.Error, actual.Error);
    Assert.Domain.Equal(expected.DeployedBy, actual.DeployedBy);
    Assert.Domain.Equal(expected.DeployedOn, actual.DeployedOn);
    Assert.Domain.Equal(expected.CreatedOn, actual.CreatedOn);
    Assert.Domain.Equal(expected.UpdatedOn, actual.UpdatedOn);
  }

  public void Equal(Content.Object expected, Content.Object? actual)
  {
    Assert.Present(actual);
    Assert.Domain.Equal(expected.Id, actual.Id);
    Assert.Domain.Equal(expected.Path, actual.Path);
    Assert.Domain.Equal(expected.Blake3, actual.Blake3);
    Assert.Domain.Equal(expected.MD5, actual.MD5);
    Assert.Domain.Equal(expected.Sha256, actual.Sha256);
    Assert.Domain.Equal(expected.ContentLength, actual.ContentLength);
    Assert.Domain.Equal(expected.ContentType, actual.ContentType);
    Assert.Domain.Equal(expected.CreatedOn, actual.CreatedOn);
    Assert.Domain.Equal(expected.UpdatedOn, actual.UpdatedOn);
  }

  //-----------------------------------------------------------------------------------------------

  public void Equal<T>(List<T>? expected, List<T>? actual)
  {
    if (expected is null)
    {
      Assert.Absent(actual);
    }
    else
    {
      Assert.Present(actual);
      Assert.Domain.Equal(expected.Count, actual.Count);
      for (var n = 0; n < expected.Count; n++)
      {
        Assert.Domain.Equal(AsDynamic(expected[n]), AsDynamic(expected[n]));
      }
    }
  }

  //-----------------------------------------------------------------------------------------------

  public void Equal(GitHub.Release expected, GitHub.Release actual)
  {
    Assert.Equal(expected.Id, actual.Id);
    Assert.Equal(expected.Name, actual.Name);
    Assert.Equal(expected.TagName, actual.TagName);
    Assert.Equal(expected.PreRelease, actual.PreRelease);
    Assert.Equal(expected.Draft, actual.Draft);
    Assert.Equal(expected.PublishedOn, actual.PublishedOn);
    Assert.Equal(expected.Body, actual.Body);
    Assert.Equal(expected.Assets, actual.Assets);
  }

  //-----------------------------------------------------------------------------------------------

  private dynamic AsDynamic<T>(T value)
  {
    Assert.NotNull(value);
    return (dynamic) value;
  }

  //-----------------------------------------------------------------------------------------------

  public void CacheAbsent(ICache cache, string key)
  {
    Assert.Null(cache.GetOrDefault<object>(key));
  }

  public T CachePresent<T>(ICache cache, string key)
  {
    var value = cache.GetOrDefault<T>(key);
    Assert.NotNull(value);
    return value;
  }

  public void CachePresent(ICache cache, string key)
  {
    Assert.True(cache.Contains(key));
  }

  //-----------------------------------------------------------------------------------------------

  public void Files(string[] expected, DomainTest test)
  {
    Files(expected, test.FileStore);
  }

  public void Files(string[] expected, TestFileStore store)
  {
    Assert.Equal(expected.Order(), store.ListFileNames());
  }

  public void DirectoryPresent(TestFileStore store, string path)
  {
    Assert.True(store.ContainsDirectory(path));
  }

  public void DirectoryAbsent(TestFileStore store, string path)
  {
    Assert.False(store.ContainsDirectory(path));
  }

  public void FilePresent(TestFileStore store, string path)
  {
    Assert.True(store.ContainsFile(path));
  }

  public void FileAbsent(TestFileStore store, string path)
  {
    Assert.False(store.ContainsFile(path));
  }

  public void NoFilesSaved(DomainTest test)
  {
    Assert.Equal([], test.FileStore.ListFileNames());
  }

  //-----------------------------------------------------------------------------------------------

  public TestMinions.Entry Enqueued(DomainTest test) =>
    Enqueued(test.Minions);

  public D Enqueued<M, D>(DomainTest test) where M : IMinion<D> where D : class =>
    Enqueued<M, D>(test.Minions);

  public void NoJobsEnqueued(DomainTest test) =>
    NoJobsEnqueued(test.Minions);

  public void NoMoreJobsEnqueued(DomainTest test) =>
    NoJobsEnqueued(test.Minions);

  //-----------------------------------------------------------------------------------------------

  public TestMinions.Entry Enqueued(TestMinions minions) =>
    minions.AssertEnqueued();

  public D Enqueued<M, D>(TestMinions minions) where M : IMinion<D> where D : class =>
    minions.AssertEnqueued<M, D>();

  public void NoJobsEnqueued(TestMinions minions) =>
    minions.AssertEmpty();

  //-----------------------------------------------------------------------------------------------
}