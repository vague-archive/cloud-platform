namespace Void.Platform.Web;

[LoadCurrentGame]
public class GameShareHelpPage : BasePage
{
  private Config Config { get; init; }
  public GameShareHelpPage(Config config)
  {
    Config = config;
  }

  public string DeployScriptCode
  {
    get
    {
      return $$"""
        #!/bin/bash

        LABEL="${1}"
        TOKEN="${HOME}/.config/void/token"
        BUILD="${BUILD:-dist}"
        BUNDLE="${BUILD}.tgz"
        ENDPOINT="{{Config.Web.PublicUrl}}api/{{Current.Organization.Slug}}/{{Current.Game.Slug}}/share"

        if [[ ! -n ${VOID_ACCESS_TOKEN} ]]; then
          if [[ -f "${TOKEN}" ]]; then
            VOID_ACCESS_TOKEN=$(cat "${TOKEN}")
          else
            echo "No access token found. Find your access token on your profile page at {{Config.Web.PublicUrl}}profile and save it to ~/.config/void/token"
            exit 1
          fi
        fi

        bun run build --base "./"
        if [ $? -ne 0 ]; then
          echo "Build failed, not uploading to server."
          exit 1
        fi
        tar -czf ${BUNDLE} -C ${BUILD} .

        echo "Uploading build to {{Config.Web.PublicUrl}}"
        OUTPUT=$(curl -s -X POST --fail-with-body --connect-timeout 30 --max-time 300 -H "X-Deploy-Label: ${LABEL}" -H "Authorization: Bearer ${VOID_ACCESS_TOKEN}" -T "${BUNDLE}" ${ENDPOINT})
        if [ $? -ne 0 ]; then
          echo $OUTPUT
          echo "Sorry, upload failed, please try again in a few minutes or contact support@void.dev"
          exit 1
        fi

        rm ${BUNDLE}

        echo $OUTPUT
        """;
    }
  }

  public string GitHubActionCode
  {
    get
    {
      return $$$"""
        name: Deploy

        on:
          push:
            branches: [main]

        jobs:
          build:
            runs-on: ubuntu-latest
            steps:
              - name: Checkout Repo
                uses: actions/checkout@v4

              - name: Build, Package, and Deploy to the Web
                uses: vaguevoid/actions/share/on/web@v1
                with:
                  organization: {{{Current.Organization.Slug}}}
                  game:         {{{Current.Game.Slug}}}
                  label:        main
                  token:        ${{ secrets.VOID_ACCESS_TOKEN }}
        """;
    }
  }

  public IActionResult OnGet()
  {
    return Page();
  }
}