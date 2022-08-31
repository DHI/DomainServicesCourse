import { useEffect, useState } from "react";
import { Theme, makeStyles } from "@material-ui/core";
import { observer } from "mobx-react";
import {
  DataSource,
  fetchTimeseriesValues,
  BaseChart,
} from "@dhi/react-components";
import { useStore } from "../index";

type TimeseriesData = Array<Array<any>>;

interface TimeseriesResponse {
  data: TimeseriesData;
  id: string;
}

const styles = makeStyles((theme: Theme) => ({
  layout: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
  },
  chart: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    width: "100%",
  },
}));

const Sidebar = observer(() => {
  const appStore = useStore();
  const classes = styles();

  const [chartConfig, setChartConfig] = useState<any>();

  // when a point is selected, fetch timeseries and display a chart
  useEffect(() => {
    if (!appStore.selectedPoint?.timeseries) {
      return;
    }

    const datasource: DataSource = {
      host: process.env.REACT_APP_API_ENDPOINT_URL as string,
      connection: process.env.REACT_APP_TS_CONNECTION as string,
      ids: [appStore.selectedPoint?.timeseries],
    };

    fetchTimeseriesValues(
      [datasource],
      appStore.session?.accessToken as string
    ).subscribe((data: TimeseriesResponse[]) => {
      const nameTextStyle = {
        fontSize: 12,
        padding: 8,
      };

      const options = {
        tooltip: {},
        legend: {
          data: ["Water Level [m]"],
        },
        xAxis: {
          type: "category",
          data: data[0].data.map((valuePair) => {
            return valuePair[0];
          }),
        },
        yAxis: {
          name: "Water Level [m]",
          nameLocation: "center",
          nameTextStyle,
        },
        series: [
          {
            name: "Water Level [m]",
            type: "line",
            data: data[0].data.map((valuePair) => {
              return valuePair[1];
            }),
          },
        ],
        dataZoom: [
          {
            type: "slider",
          },
          {
            type: "inside",
          },
        ],
      };

      setChartConfig(options);
    });
  }, [appStore.selectedPoint?.timeseries, appStore.session?.accessToken]);

  return (
    <div className={classes.layout}>
      <h4>
        {appStore.selectedPoint
          ? appStore.selectedPoint.name
          : "no point selected"}
      </h4>
      <div className={classes.chart}>
        {chartConfig ? (
          <BaseChart
            className="standard_chart"
            chartHeightFunc={() => window.innerHeight * 0.4}
            options={chartConfig}
          />
        ) : (
          "no data"
        )}
      </div>
    </div>
  );
});

export default Sidebar;
