namespace OpenGaugeClient.Client
{
    public static class SimVarHelper
    {
        public static List<SimVarDef> GetSimVarDefsToSubscribeTo(Config config, string? vehicleName, bool includeSkipped = false)
        {
            var simSimVarDefs = new List<SimVarDef>();

            if (config.Panels == null || config.Panels.Count == 0)
                throw new Exception("No panels");

            foreach (var panel in config.Panels)
            {
                if (!PanelHelper.GetIsPanelVisible(panel, vehicleName))
                    continue;

                if (panel.Skip == true && includeSkipped != true)
                    continue;

                var gaugeRefs = panel.Gauges;

                foreach (var gaugeRef in gaugeRefs)
                {
                    var gauge = gaugeRef.Gauge;

                    if (gauge == null)
                        throw new Exception("No gauge");

                    var layers = gauge.Layers;

                    foreach (var layer in layers)
                    {
                        void AddVar(SimVarConfig varConfig)
                        {
                            simSimVarDefs.Add(new SimVarDef { Name = varConfig.Name, Unit = varConfig.Unit, Debug = layer.Debug == true });
                        }

                        if (layer.Text?.Var is not null)
                            AddVar(layer.Text.Var!);

                        if (layer.Transform is { } transform)
                        {
                            if (transform.Rotate?.Var is not null)
                                AddVar(transform.Rotate.Var!);

                            if (transform.TranslateX?.Var is not null)
                                AddVar(transform.TranslateX.Var!);

                            if (transform.TranslateY?.Var is not null)
                                AddVar(transform.TranslateY.Var!);

                            if (transform.Path?.Var is not null)
                                AddVar(transform.Path.Var!);
                        }
                    }
                }
            }

            return simSimVarDefs;
        }
    }
}