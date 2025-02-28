local rx = require('rx')
local backend = require('Backend')
local launcherPlugin = require('Plugins.Launcher')

local panel = document;

local function updatePanelStatus()
  panel.WantsAttention = false
  panel:OnShow('-', updatePanelStatus)
end

panel:OnShow('+', updatePanelStatus)

local state = rx:CreateState({
  isDownloading = false,
  downloadProgress = 0
})

launcherPlugin.UpdateChecker:OnUpdateProgressChanged('+', function (s, e)
  state.downloadProgress = e.Value
end)

function ModalView(state)
  local updateText = string.format("Chorizite version %s is available!", launcherPlugin.UpdateChecker.LatestVersion:ToString());
  return rx:Div({
    class="modal panel"
  }, {
    rx:P(updateText),
    rx:Div({
      class = {
        ["progress"] = true,
        ["hidden"] = not state.isDownloading
      }
    },
    {
      rx:Progress({
        value = state.downloadProgress,
        class = "horizontal"
      })
    }),
    rx:Button("Update", {
      disabled = state.isDownloading,
      onclick = function()
        state.isDownloading = true
        launcherPlugin.UpdateChecker:Update();
      end
    }),
    rx:Button("Cancel", {
      onclick = function()
        panel:Hide()
      end
    }),
  })
end

document:Mount(function() return ModalView(state) end, "#modal")