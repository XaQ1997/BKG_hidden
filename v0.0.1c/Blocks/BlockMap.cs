using UnityEngine;

public class BlockMap:Object
{
    private Block[,] blocks = new Block[16, 16];

    public void Swap(Block block, int categoryId, int typeId)
    {
        blocks[categoryId, typeId] = block;
    }

    public Block Show(int categoryId, int typeId)
    {
        return blocks[categoryId, typeId];
    }

    //public Block Show(string _name)
    //{
    //    var block = new Block();

    //    for(int i=0;i<16;++i)
    //        for(int j=0;j<16;++j)
    //            if(blocks[i, j].Name()==_name)
    //            {
    //                block = blocks[i, j];
    //                break;
    //            }

    //    return block;
    //}
}