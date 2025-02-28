
local rx = require('rx')
local ui = require('Plugins.RmlUi')
local PluginManager = require('PluginManager')

local initialPlugins = {}

local state = rx:CreateState({
  Plugins = {},
  Cheating = 1
})

local function updatePanelState(panel)
  if not panel.ShowInBar then
    state.Plugins[panel.Name] = nil
  else
    state.Plugins[panel.Name] = state.Plugins[panel.Name] or {}
    state.Plugins[panel.Name].icon = panel.IconUri
    state.Plugins[panel.Name].isVisible = panel.IsVisible
    state.Plugins[panel.Name].wantsAttention = panel.WantsAttention
  end
  state.Cheating = state.Cheating + 1
end

local function LoadPlugins()
  for i=0,ui.PanelManager.Panels.Count-1 do
    if ui.PanelManager.Panels[i].ShowInBar then
      updatePanelState(ui.PanelManager.Panels[i])
    end
  end
  state.Cheating = state.Cheating + 1
end

PluginManager:OnPluginsLoaded('+', function(s, e)
  LoadPlugins()
end)
LoadPlugins()

ui.PanelManager:OnPanelAdded('+', function(s, e)
  updatePanelState(e.Panel)
end)

ui.PanelManager:OnPanelRemoved('+', function(s, e)
  state.Plugins[e.Panel.Name] = nil
  state.Cheating = state.Cheating + 1
end)

ui.PanelManager:OnPanelVisibilityChanged('+', function(s, e)
  updatePanelState(e.Panel)
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
            res[#res + 1] = rx:Div({
              class = {
                ["pulse"] = panelInfo.wantsAttention
              }
            }, {
              rx:Button({
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
            })
          end
          return res
        end)
      })
    })
  })
end

document:Mount(function() return PluginsBarView(state) end, "#pluginsbar-wrapper")