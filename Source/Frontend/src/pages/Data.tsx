import { Theme, makeStyles } from "@material-ui/core";
import { observer } from "mobx-react";
import { useStore } from "../index";
import MapView from "../components/MapView";

const styles = makeStyles((theme: Theme) => ({
  // 60px high app bar at top of page
  appBar: {
    position: "fixed",
    top: 0,
    left: 0,
    right: 0,
    height: 60,
    backgroundColor: "lightgray",
  },
  // rest of the page is used for map container
  mapContainer: {
    position: "fixed",
    top: 60,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: "gray",
  },
  // side bar hovers over the map container
  sideBar: {
    position: "absolute",
    top: 0,
    right: 0,
    bottom: 0,
    width: 300,
    backgroundColor: "darkgray",
    opacity: 0.8,
  },
}));

const Data = observer(() => {
  const appStore = useStore();
  const classes = styles();

  return (
    <>
      <div className={classes.appBar}>
        <p>Top Bar</p>
      </div>
      <div className={classes.mapContainer}>
        <MapView />
        <div className={classes.sideBar}>
          <p>Side bar</p>
        </div>
      </div>
    </>
  );
});

export default Data;
