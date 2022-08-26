import { useStore } from "../index";
import { observer } from "mobx-react";

const Data = observer(() => {
  const appStore = useStore();

  return (
    <>
      <div>Data page</div>
      <div>User: {appStore.session?.user.toString()}</div>
    </>
  );
});

export default Data;
