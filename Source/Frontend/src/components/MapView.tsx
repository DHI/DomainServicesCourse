import { useState } from "react";
import "mapbox-gl/dist/mapbox-gl.css";
import DeckGL from "@deck.gl/react";
import { GeoJsonLayer } from "@deck.gl/layers";
import StaticMap from "react-map-gl";
import { useStore } from "../index";

const MapView = () => {
  const appStore = useStore();

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
    const { Name: name, timeseries } = info.object.properties;
    appStore.setSelectedPoint({ name, timeseries });
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
