Run Elsa.Latest for v2.0
Run Elsa.LTS for v1.5

Replace the SQL Server connection string in `appsettings.json`

In v2.0, in order to run customized frontend:
1. Build the elsa-workflow-studio from Elsa's repository.
2. Create a `wwwroot` folder in the Elsa.Latest project.
3. Paste the `elsa-worflow-studio` folder generated in step 1, inside the `wwwroot` folder.
4. Replace the hrefs in the `_Host.cshtml` as shown below.
```
@page "/"
@{
    var serverUrl = $"{Request.Scheme}://{Request.Host}";
}
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Elsa Workflows</title>
    <link rel="icon" type="image/png" sizes="32x32" href="/elsa-workflows-studio/assets/images/favicon-32x32.png">
    <link rel="icon" type="image/png" sizes="16x16" href="/elsa-workflows-studio/assets/images/favicon-16x16.png">
    <link rel="stylesheet" href="/elsa-workflows-studio/assets/fonts/inter/inter.css">
    <link rel="stylesheet" href="/elsa-workflows-studio/assets/styles/tailwind.css">
    <script src="/_content/Elsa.Designer.Components.Web/monaco-editor/min/vs/loader.js"></script>
    <script type="module" src="/elsa-workflows-studio/elsa-workflows-studio.esm.js"></script>
</head>
<body class="h-screen" style="background-size: 30px 30px; background-image: url(/elsa-workflows-studio/assets/images/tile.png); background-color: #FBFBFB;">
    <elsa-studio-root server-url="@serverUrl" monaco-lib-path="_content/Elsa.Designer.Components.Web/monaco-editor/min"></elsa-studio-root>
</body>
</html>
```