namespace Void.Platform.Domain;

public partial class Account
{
  //-----------------------------------------------------------------------------------------------

  public enum GamePurpose
  {
    Game,
    Tool,
  }

  //-----------------------------------------------------------------------------------------------

  public record Game
  {
    public long Id { get; set; }
    public required long OrganizationId { get; set; }
    public required GamePurpose Purpose { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public required bool IsArchived { get; set; }
    public Instant? ArchivedOn { get; set; }
    public required Instant CreatedOn { get; set; }
    public required Instant UpdatedOn { get; set; }

    [DatabaseIgnore] public Organization? Organization { get; set; }
    [DatabaseIgnore] public List<Share.Branch>? Branches { get; set; }
  }

  public const string GameFields = @"
    games.id              as Id,
    games.organization_id as OrganizationId,
    games.purpose         as Purpose,
    games.name            as Name,
    games.slug            as Slug,
    games.description     as Description,
    games.archived        as IsArchived,
    games.archived_on     as ArchivedOn,
    games.created_on      as CreatedOn,
    games.updated_on      as UpdatedOn
  ";

  //===============================================================================================
  // GET GAMES
  //===============================================================================================

  public Game? GetGame(long id)
  {
    return Db.QuerySingleOrDefault<Game>(@$"
      SELECT {GameFields}
      FROM games
      WHERE id = @Id
    ", new { Id = id });
  }

  public Game? GetGame(Organization org, string slug)
  {
    return Db.QuerySingleOrDefault<Game>(@$"
      SELECT {GameFields}
      FROM games
      WHERE organization_id = @OrganizationId
        AND slug = @Slug
    ", new
    {
      OrganizationId = org.Id,
      Slug = Format.Slugify(slug)
    });
  }

  //-----------------------------------------------------------------------------------------------

  public List<Game> GetGamesForOrganization(Organization org)
  {
    return Db.Query<Game>(@$"
      SELECT {GameFields}
        FROM games
       WHERE organization_id = @OrganizationId
    ORDER BY name
    ", new
    {
      OrganizationId = org.Id
    });
  }

  //-----------------------------------------------------------------------------------------------

  public List<Game> GetPublicTools()
  {
    var tools = Db.Query<Game>(@$"
      SELECT {GameFields}
        FROM games
       WHERE purpose = @Purpose
         AND archived IS FALSE
    ORDER BY name
    ", new
    {
      Purpose = GamePurpose.Tool
    });

    var orgs = GetOrganizations(tools.Select(t => t.OrganizationId));
    var branches = App.Share.GetActiveBranchesForGame(tools);

    foreach (var tool in tools)
    {
      tool.Organization = orgs[tool.OrganizationId];
      tool.Branches = branches[tool.Id];
    }

    return tools;
  }

  //===============================================================================================
  // CREATE GAME
  //===============================================================================================

  public class CreateGameCommand
  {
    public string Name { get; set; } = "";
    public string? Slug { get; set; } = null;
    public string? Description { get; set; } = null;
    public GamePurpose Purpose { get; set; } = GamePurpose.Game;

    public class Validator : AbstractValidator<CreateGameCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Name)
          .NotEmpty().WithMessage(Validation.IsMissing)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));

        RuleFor(cmd => cmd.Slug)
          .NotEmpty()
          .When(cmd => !String.IsNullOrWhiteSpace(cmd.Name))
          .WithMessage(Validation.IsMissing);

        RuleFor(cmd => cmd.Slug)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));
      }
    }
  }

  public Result<Game> CreateGame(Organization org, CreateGameCommand cmd)
  {
    cmd.Slug = Format.Slugify(cmd.Slug ?? cmd.Name ?? "");
    var result = new CreateGameCommand.Validator().Validate(cmd);
    if (!result.IsValid)
      return Validation.Fail(result);

    RuntimeAssert.Present(cmd.Name);
    RuntimeAssert.Present(cmd.Slug);

    var game = new Game
    {
      Id = 0,
      OrganizationId = org.Id,
      Purpose = cmd.Purpose,
      Name = cmd.Name,
      Slug = cmd.Slug,
      Description = cmd.Description,
      IsArchived = false,
      ArchivedOn = null,
      CreatedOn = Now,
      UpdatedOn = Now,
    };

    try
    {
      game.Id = Db.Insert(@"
        INSERT INTO games (
          organization_id,
          purpose,
          name,
          slug,
          description,
          archived,
          archived_on,
          created_on,
          updated_on
        ) VALUES (
          @OrganizationId,
          @Purpose,
          @Name,
          @Slug,
          @Description,
          @IsArchived,
          @ArchivedOn,
          @CreatedOn,
          @UpdatedOn
        )
      ", game);
    }
    catch (MySqlConnector.MySqlException ex) when (ex.ErrorCode == MySqlConnector.MySqlErrorCode.DuplicateKeyEntry)
    {
      if (ex.Message.Contains("games.games_slug_index"))
        return Validation.Fail("name", "already taken for this organization");
      else
        throw;
    }

    return Result.Ok(game);
  }

  //===============================================================================================
  // UPDATE GAME
  //===============================================================================================

  public class UpdateGameCommand
  {
    public string Name { get; set; } = "";
    public string? Slug { get; set; } = null;
    public string? Description { get; set; } = null;

    public class Validator : AbstractValidator<UpdateGameCommand>
    {
      public Validator()
      {
        RuleFor(cmd => cmd.Name)
          .NotEmpty().WithMessage(Validation.IsMissing)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));

        RuleFor(cmd => cmd.Slug)
          .NotEmpty()
          .When(cmd => !String.IsNullOrWhiteSpace(cmd.Name))
          .WithMessage(Validation.IsMissing);

        RuleFor(cmd => cmd.Slug)
          .MaximumLength(255).WithMessage(Validation.TooLong(255));
      }
    }
  }

  public Result<Game> UpdateGame(Game game, UpdateGameCommand cmd)
  {
    cmd.Slug = Format.Slugify(cmd.Slug ?? cmd.Name);
    var result = new UpdateGameCommand.Validator().Validate(cmd);
    if (!result.IsValid)
      return Validation.Fail(result);

    game.Name = cmd.Name;
    game.Slug = cmd.Slug;
    game.Description = cmd.Description;
    game.UpdatedOn = Now;

    try
    {
      var numRows = Db.Execute(@"
        UPDATE games
           SET name = @Name,
               slug = @Slug,
               description = @Description,
               updated_on = @UpdatedOn
         WHERE id = @Id
      ", game);
      RuntimeAssert.True(numRows == 1);
      return Result.Ok(game);
    }
    catch (MySqlConnector.MySqlException ex) when (ex.ErrorCode == MySqlConnector.MySqlErrorCode.DuplicateKeyEntry)
    {
      if (ex.Message.Contains("games.games_slug_index"))
        return Validation.Fail("name", "already taken for this organization");
      else
        throw;
    }
  }

  //===============================================================================================
  // ARCHIVE and DELETE GAME
  //===============================================================================================

  public Result<Game> ArchiveGame(Game game, bool isArchived = true)
  {
    game.IsArchived = isArchived;
    game.ArchivedOn = isArchived ? Now : null;
    var numRows = Db.Execute(@"
      UPDATE games
         SET archived = @IsArchived,
             archived_on = @ArchivedOn
       WHERE id = @Id
    ", game);
    RuntimeAssert.True(numRows == 1);
    return Result.Ok(game);
  }

  public Result<Game> RestoreGame(Game game)
  {
    return ArchiveGame(game, false);
  }

  //-----------------------------------------------------------------------------------------------

  public Result<Game> DeleteGame(Game game)
  {
    var numRows = Db.Execute(@"
      DELETE FROM games
            WHERE id = @Id
    ", new { Id = game.Id });
    RuntimeAssert.True(numRows == 1);
    MoveToTrashMinion.Enqueue(Minions, Share.DeployPath(game), $"{Format.Enum(game.Purpose)} deleted");
    return Result.Ok(game);
  }

  //-----------------------------------------------------------------------------------------------
}