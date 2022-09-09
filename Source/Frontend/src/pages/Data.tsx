import { Theme, makeStyles, Button, Typography } from "@material-ui/core";
import { observer } from "mobx-react";

import { useStore } from "../index";
import MapView from "../components/MapView";
import Sidebar from "../components/SideBar";

const styles = makeStyles((theme: Theme) => ({
  // 60px high app bar at top of page
  appBar: {
    position: "fixed",
    top: 0,
    left: 0,
    right: 0,
    height: 60,
    backgroundColor: "lightgray",
    display: "flex",
    alignItems: "center",
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
    top: 60,
    right: 0,
    bottom: 0,
    width: 400,
    backgroundColor: "darkgray",
    opacity: 0.95,
  },
  spacer: {
    flex: 1,
  },
  logout: {
    marginRight: 20,
  },
  buttonLabel: {},
}));

const Data = observer(() => {
  const appStore = useStore();
  const classes = styles();

  return (
    <>
      <nav className={classes.appBar}>
        <div className={classes.spacer} />
        <h3>
          Domain Services Enabler Course - Sample Web App - {appStore.version}
        </h3>
        <div className={classes.spacer} />
        <Button
          className={classes.logout}
          onClick={() => appStore.resetAppState()}
        >
          <Typography className={classes.buttonLabel}>Log Out</Typography>
        </Button>
      </nav>
      <main className={classes.mapContainer}>
        <MapView />
      </main>
      <aside className={classes.sideBar}>
        <Sidebar />
      </aside>
    </>
  );
});

export default Data;
