import { useState } from "react";
import { Theme, makeStyles } from "@material-ui/core";
import { observer } from "mobx-react";

import "mapbox-gl/dist/mapbox-gl.css";
import DeckGL from "@deck.gl/react";
import { GeoJsonLayer } from "@deck.gl/layers";
import StaticMap from "react-map-gl";

const styles = makeStyles((theme: Theme) => ({
  mapContainer: {},
}));

const MapView = () => {
  const classes = styles();

  const [hoverInfo, setHoverInfo] = useState<any>();
  const [viewState, setViewState] = useState({
    latitude: 0,
    longitude: 0,
    zoom: 2,
    bearing: 0,
    pitch: 0,
  });

  const onViewStateChange = (args: {
    viewState: any;
    interactionState: any;
    oldViewState: any;
  }) => {
    const { viewState } = args;
    setViewState({
      ...viewState,
    });
  };

  const handlePointClick = (info: any, event: any) => {
    console.log(info);
    console.log(event);
  };

  const handleHover = (info: any) => {
    setHoverInfo(info);
  };

  return (
    <>
      <DeckGL
        controller
        layers={[
          new GeoJsonLayer({
            id: "geojson-layer",
            data: "/points.geojson",
            pickable: true,
            stroked: false,
            filled: true,
            extruded: true,
            pointType: "circle",
            getPointRadius: 10,
            pointRadiusUnits: "pixels",
            lineWidthScale: 20,
            getFillColor: [160, 160, 180, 200],
            getLineColor: [160, 160, 180, 200],
            onClick: handlePointClick,
            onHover: handleHover,
          }),
        ]}
        viewState={viewState}
        onViewStateChange={onViewStateChange}
        getCursor={() => "pointer"}
      >
        {hoverInfo?.object && (
          <div
            style={{
              position: "absolute",
              zIndex: 1,
              pointerEvents: "none",
              left: hoverInfo.x,
              top: hoverInfo.y - 15,
            }}
          >
            {hoverInfo.object.properties.Name}
          </div>
        )}
        <StaticMap
          key="map"
          mapboxApiAccessToken={process.env.REACT_APP_MAPBOX_TOKEN as string}
        />
      </DeckGL>
    </>
  );
};

export default MapView;
