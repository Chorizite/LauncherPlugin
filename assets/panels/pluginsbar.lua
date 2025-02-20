
local rx = require('rx')
local ui = require('Plugins.RmlUi')
local PluginManager = require('PluginManager')

local initialPlugins = {}

local state = rx:CreateState({
  Plugins = {},
  Cheating = 1
})

local function LoadPlugins()
  for i=0,ui.PanelManager.Panels.Count-1 do
    if ui.PanelManager.Panels[i].ShowInBar then
      state.Plugins[ui.PanelManager.Panels[i].Name] = {
        ["isVisible"] = ui.PanelManager.Panels[i].IsVisible,
        ["icon"] = ui.PanelManager.Panels[i].IconUri
      }
    end
  end
  state.Cheating = state.Cheating + 1
end

PluginManager:OnPluginsLoaded('+', function(s, e)
  LoadPlugins()
end)
LoadPlugins()

ui.PanelManager:OnPanelAdded('+', function(s, e)
  if e.Panel.ShowInBar then
    state.Plugins[e.Panel.Name] = state.Plugins[e.Panel.Name] or {
      ["icon"] = e.Panel.IconUri
    }
    state.Plugins[e.Panel.Name].isVisible = e.Panel.IsVisible
    state.Cheating = state.Cheating + 1
  end
end)

ui.PanelManager:OnPanelRemoved('+', function(s, e)
  state.Plugins[e.Panel.Name] = nil
  state.Cheating = state.Cheating + 1
end)

ui.PanelManager:OnPanelVisibilityChanged('+', function(s, e)
  if e.Panel.ShowInBar then
    state.Plugins[e.Panel.Name] = state.Plugins[e.Panel.Name] or {}
    state.Plugins[e.Panel.Name].isVisible = e.Panel.IsVisible
    state.Plugins[e.Panel.Name].icon = e.Panel.IconUri
    state.Cheating = state.Cheating + 1
  end
end)

local PluginsBarView = function(state)
  return rx:Div({ class="panel" }, {
    rx:Div({ class="panel-inner" }, {
      rx:Handle({
        move_target="#document"
      }, {
        rx:Div({ id="handle" })
      }),
      rx:Div({ class="game-icons"}, {
        rx:Div({
          class="plugin-icons",
          count = state.Cheating
        }, function()
          local res = {}
          for panelName,panelInfo in pairs(state.Plugins) do
            res[#res + 1] = rx:Button({
              class={
                icon = true,
                visible = panelInfo.isVisible
              },
              onclick=function()
                if panelInfo.isVisible then
                  ui.PanelManager:GetPanel(panelName):Hide()
                else
                  ui.PanelManager:GetPanel(panelName):Show()
                  ui.PanelManager:GetPanel(panelName):PullToFront()
                end
              end
            }, function()
              if panelInfo.icon ~= nil then
                return {
                  rx:Div({ class="overlay" }, {
                    rx:Img({ class="overlay", src=panelInfo.icon })
                  })
                }
              else
                return {
                  rx:Div({ class="overlay" }, string.sub(panelName, 1, 1))
                }
              end
            end)
          end
          return res
        end)
      })
    })
  })
end

document:Mount(function() return PluginsBarView(state) end, "#pluginsbar-wrapper")